using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.InferenceEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

public class ARRawCameraYolo : MonoBehaviour
{
    [Header("Debug Config")]
    public bool debugMode = true; 

    [Header("AR Components")]
    public ARCameraManager cameraManager;

    [Header("Model Config")]
    public ModelAsset modelAsset;
    public TextAsset classesFile;
    [HideInInspector] public string[] classLabels;

    [Range(0f, 1f)] public float confidenceThreshold = 0.4f;
    [Range(0f, 1f)] public float iouThreshold = 0.45f;

    [Header("UI Output")]
    public RawImage outputImage; // Ảnh kết quả vẽ bounding box
    public RawImage inputDebugPreview; // (MỚI) Ảnh raw từ camera để debug
    public TMP_Text resultText; // Text hiển thị thông số

    const int INPUT_SIZE = 640; // Kích thước model yêu cầu (YOLOv8 thường là 640)
    private Model model;
    private Worker worker;
    private Tensor<float> inputTensor;
    private Texture2D cameraTex; // Lưu biến này để dùng lại

    public struct Detection {
        public int classId; public string labelName; public float score; public Rect box;
    }

    void Start()
    {
        if (debugMode) Debug.Log("--- ARRawCameraYolo Initializing ---");

        // Load Classes
        if (classesFile != null)
            classLabels = classesFile.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        // Load Model
        if (modelAsset != null)
        {
            model = ModelLoader.Load(modelAsset);
            worker = new Worker(model, BackendType.CPU); // Thử GPU cho nhanh
        }

        // Init Tensor
        inputTensor = new Tensor<float>(new TensorShape(1, 3, INPUT_SIZE, INPUT_SIZE));
    }

    public void CaptureFromCamera()
    {
        ProcessCameraImage();
    }

    void ProcessCameraImage()
    {
        // 1. Acquire Image
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            if (resultText) resultText.text = "❌ Error: Cannot acquire CPU Image";
            return;
        }

        // --- FIX LOGIC KÍCH THƯỚC ẢNH ---
        // Không chia 2 cứng nhắc nữa. Giữ nguyên hoặc resize thông minh.
        // Ví dụ: Nếu ảnh quá to (4K), ta downscale để đỡ lag, nhưng không được nhỏ hơn INPUT_SIZE (640).
        
        int targetW = image.width;
        int targetH = image.height;
        
        // Nếu ảnh quá lớn (>2000px), có thể chia đôi để tối ưu hiệu năng (tùy chọn)
        // Nhưng đảm bảo cạnh nhỏ nhất vẫn >= 640
        if (targetW > 2000 || targetH > 2000) 
        {
            targetW /= 2; 
            targetH /= 2;
        }

        // 2. Conversion Params
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(targetW, targetH), // Kích thước mới
            outputFormat = TextureFormat.RGBA32,
            // MirrorY để lật ảnh cho đúng chiều trục Y của Unity UI
            transformation = XRCpuImage.Transformation.MirrorY
        };

        // Tạo buffer và convert
        int size = image.GetConvertedDataSize(conversionParams);
        var buffer = new NativeArray<byte>(size, Allocator.Temp);
        image.Convert(conversionParams, buffer);
        image.Dispose(); // Giải phóng ảnh gốc XRCpuImage ngay

        // 3. Create/Update Texture
        // Tối ưu: Nếu texture cũ cùng kích thước thì dùng lại, đỡ new nhiều rác bộ nhớ
        if (cameraTex == null || cameraTex.width != targetW || cameraTex.height != targetH)
        {
            if(cameraTex != null) Destroy(cameraTex);
            cameraTex = new Texture2D(targetW, targetH, conversionParams.outputFormat, false);
        }

        cameraTex.LoadRawTextureData(buffer);
        cameraTex.Apply();
        cameraTex = RotateTexture(cameraTex);
        buffer.Dispose();

        // --- (MỚI) HIỂN THỊ ẢNH RAW DEBUG ---
        if (inputDebugPreview != null)
        {
            inputDebugPreview.texture = cameraTex;
            inputDebugPreview.gameObject.SetActive(true);
            
            // Giữ đúng tỷ lệ ảnh trên UI
            var fitter = inputDebugPreview.GetComponent<AspectRatioFitter>();
            if (fitter) fitter.aspectRatio = (float)targetW / targetH;
        }

        // 4. Resize về 640x640 (Cẩn thận vấn đề méo ảnh - Squashing)
        // Graphics.Blit mặc định sẽ kéo dãn toàn bộ ảnh vào hình vuông 640x640.
        // Nếu muốn chuẩn, bạn cần crop center. Ở đây tạm thời ta Blit full (chấp nhận méo tí).
        RenderTexture rt = RenderTexture.GetTemporary(INPUT_SIZE, INPUT_SIZE, 0);
        Graphics.Blit(cameraTex, rt);

        // Chuyển sang Tensor
        TextureConverter.ToTensor(rt, inputTensor, new TextureTransform().SetDimensions(INPUT_SIZE, INPUT_SIZE, 3).SetChannelSwizzle(ChannelSwizzle.RGBA));
        
        // --- (MỚI) DEBUG TEXT ---
        string debugInfo = $"<b>Camera Info:</b>\n" +
                           $"Raw: {image.width}x{image.height}\n" +
                           $"Tex: {targetW}x{targetH}\n" +
                           $"AI Input: {INPUT_SIZE}x{INPUT_SIZE}\n" +
                           $"----------------\n";

        // 5. Inference
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        
        worker.Schedule(inputTensor);
        using var outputTensor = worker.PeekOutput() as Tensor<float>;
        var readableOut = outputTensor.ReadbackAndClone();
        
        stopwatch.Stop();
        debugInfo += $"Inference Time: {stopwatch.ElapsedMilliseconds}ms\n";

        RenderTexture.ReleaseTemporary(rt); // Dọn dẹp RT

        // 6. Parse & Draw
        List<Detection> results = ParseYoloOutput(readableOut);
        
        // Hiển thị kết quả lên UI Text
        debugInfo += $"Detections: {results.Count}\n";
        foreach(var r in results) debugInfo += $"- {r.labelName} ({r.score:F2})\n";
        
        if (resultText) resultText.text = debugInfo;

        // Vẽ ô vuông (DrawBoundingBoxes dùng lại code cũ, nhưng nhớ truyền cameraTex mới)
        Texture2D finalImg = DrawBoundingBoxes(cameraTex, results);
        if (outputImage != null)
        {
            outputImage.texture = finalImg;
            outputImage.gameObject.SetActive(true);
            var fitter = outputImage.GetComponent<AspectRatioFitter>();
            if (fitter) fitter.aspectRatio = (float)targetW / targetH;
        }
    }

    // ... (Giữ nguyên các hàm ParseYoloOutput, NonMaxSuppression, DrawBoundingBoxes, GetIoU từ code trước) ...
    // Copy lại các hàm phụ trợ đó vào đây nhé.
    
    // Lưu ý sửa hàm DrawBoundingBoxes một chút để vẽ nét hơn nếu ảnh to:
    Texture2D DrawBoundingBoxes(Texture2D source, List<Detection> detections)
    {
        Texture2D tex = new Texture2D(source.width, source.height, source.format, false);
        Graphics.CopyTexture(source, tex);
        int W = tex.width; int H = tex.height;
        
        foreach (var det in detections)
        {
            int x = (int)(det.box.x * W); 
            int y = (int)((1f - det.box.y - det.box.height) * H); // Y lật ngược
            int w = (int)(det.box.width * W);
            int h = (int)(det.box.height * H); 

            // Độ dày nét vẽ tùy chỉnh theo độ phân giải ảnh
            int thickness = Mathf.Max(2, W / 150); 
            DrawRect(tex, x, y, w, h, Color.red, thickness);
        }
        tex.Apply(); return tex;
    }

    void DrawRect(Texture2D tex, int x, int y, int w, int h, Color col, int thickness)
    {
        for (int t = 0; t < thickness; t++) {
            for (int i = x; i < x + w; i++) { 
                if (i>=0 && i<tex.width && y+t < tex.height && y+t >=0) tex.SetPixel(i, y + t, col); 
                if (i>=0 && i<tex.width && y+h-t < tex.height && y+h-t >=0) tex.SetPixel(i, y + h - t, col); 
            }
            for (int j = y; j < y + h; j++) { 
                if (x+t >=0 && x+t < tex.width && j < tex.height && j >=0) tex.SetPixel(x + t, j, col); 
                if (x+w-t >=0 && x+w-t < tex.width && j < tex.height && j >=0) tex.SetPixel(x + w - t, j, col); 
            }
        }
    }
    
    // (Giữ nguyên ParseYoloOutput, NonMaxSuppression, GetIoU)
    List<Detection> ParseYoloOutput(Tensor<float> output)
    {
         var data = output.DownloadToArray();
        List<Detection> candidates = new List<Detection>();
        
        int dim1 = output.shape[1];
        int dim2 = output.shape[2];
        bool isChannelsFirst = (dim1 < dim2); 
        int numClasses = isChannelsFirst ? (dim1 - 4) : (dim2 - 4);
        int numAnchors = isChannelsFirst ? dim2 : dim1;

        for (int i = 0; i < numAnchors; i++)
        {
            float maxScore = 0f; 
            int bestClassId = -1;

            for (int c = 0; c < numClasses; c++)
            {
                int index = isChannelsFirst ? (4 + c) * numAnchors + i : i * (numClasses + 4) + (4 + c);
                if (index < data.Length && data[index] > maxScore) 
                { 
                    maxScore = data[index]; 
                    bestClassId = c; 
                }
            }

            if (maxScore > confidenceThreshold)
            {
                float cx, cy, w, h;
                if (isChannelsFirst) {
                    cx = data[0 * numAnchors + i]; cy = data[1 * numAnchors + i];
                    w = data[2 * numAnchors + i]; h = data[3 * numAnchors + i];
                } else {
                    int offset = i * (numClasses + 4);
                    cx = data[offset + 0]; cy = data[offset + 1];
                    w = data[offset + 2]; h = data[offset + 3];
                }
                
                float x = (cx - w / 2f) / INPUT_SIZE;
                float y = (cy - h / 2f) / INPUT_SIZE;

                string name = (classLabels != null && bestClassId < classLabels.Length && bestClassId >= 0) ? classLabels[bestClassId] : $"ID {bestClassId}";
                candidates.Add(new Detection { classId = bestClassId, labelName = name, score = maxScore, box = new Rect(x, y, w / INPUT_SIZE, h / INPUT_SIZE) });
            }
        }
        return NonMaxSuppression(candidates);
    }
    Texture2D RotateTexture(Texture2D originalTexture)
    {
        Color32[] original = originalTexture.GetPixels32();
        Color32[] rotated = new Color32[original.Length];
        
        int w = originalTexture.width;
        int h = originalTexture.height;

        // Thuật toán xoay 90 độ theo chiều kim đồng hồ
        for (int j = 0; j < h; j++)
        {
            for (int i = 0; i < w; i++)
            {
                // Công thức xoay 90 độ: [x, y] -> [y, w - 1 - x]
                // Lưu ý: Tọa độ mảng 1 chiều = y * Width + x
                int iRotated = (h - 1 - j) * w + i; // Logic xoay tùy thuộc chiều
                
                // Xoay -90 độ (để dựng đứng ảnh từ Landscape sang Portrait)
                rotated[i * h + (h - j - 1)] = original[j * w + i];
            }
        }

        Texture2D rotatedTex = new Texture2D(h, w, TextureFormat.RGBA32, false);
        rotatedTex.SetPixels32(rotated);
        rotatedTex.Apply();
        
        return rotatedTex;
    }

    List<Detection> NonMaxSuppression(List<Detection> boxes) {
        var sorted = boxes.OrderByDescending(b => b.score).ToList(); 
        var result = new List<Detection>(); 
        while (sorted.Count > 0) 
        {   var current = sorted[0]; 
            result.Add(current); 
            sorted.RemoveAt(0); 
            sorted.RemoveAll(b => GetIoU(current.box, b.box) > iouThreshold); 
        } 
        return result; 
    }

    float GetIoU(Rect a, Rect b) { 
        float inter = Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin)) * Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin)); 
        float union = (a.width * a.height) + (b.width * b.height) - inter;
        return inter / union; 
    }

    void OnDestroy() { worker?.Dispose(); inputTensor?.Dispose(); if(cameraTex) Destroy(cameraTex); }
}