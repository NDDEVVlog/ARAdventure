using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.XR.ARFoundation;

[UnitTitle("Filter Image By Name")]
[UnitCategory("AR Bridge")]
[TypeIcon(typeof(GameObject))]
public class FilterImageNameUnit : Unit
{
    [DoNotSerialize]
    public ValueInput inputImage;           // ARTrackedImage

    [DoNotSerialize]
    public ValueInput targetName;           // string muốn so sánh

    [DoNotSerialize]
    public ControlInput enter;

    [DoNotSerialize]
    public ControlOutput matched;           // Nếu tên trùng

    [DoNotSerialize]
    public ControlOutput notMatched;        // Nếu không trùng

    [DoNotSerialize]
    public ValueOutput imageName;           // Trả ra tên thật của ảnh (dùng debug)

    protected override void Definition()
    {
        enter = ControlInput("Enter", OnEnter);

        inputImage = ValueInput<ARTrackedImage>("AR Tracked Image");
        targetName = ValueInput<string>("Target Image Name", "MyCard");

        matched = ControlOutput("Matched");
        notMatched = ControlOutput("Not Matched");

        imageName = ValueOutput<string>("Image Name");

        Requirement(inputImage, enter);
        Requirement(targetName, enter);

        Succession(enter, matched);
        Succession(enter, notMatched);
    }

    private ControlOutput OnEnter(Flow flow)
    {
        ARTrackedImage trackedImage = flow.GetValue<ARTrackedImage>(inputImage);

        if (trackedImage == null || trackedImage.referenceImage == null)
        {
            flow.SetValue(imageName, "NULL");
            return notMatched;
        }

        string detectedName = trackedImage.referenceImage.name;
        string wantedName = flow.GetValue<string>(targetName);

        flow.SetValue(imageName, detectedName);

        if (string.Equals(detectedName, wantedName, System.StringComparison.OrdinalIgnoreCase))
        {
            return matched;
        }
        else
        {
            return notMatched;
        }
    }
}