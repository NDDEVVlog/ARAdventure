using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildScriptTrackedImage.asset", 
                 menuName = "Addressables/Content Builders/Tracked Image Build Script")]
public class TrackedImageBuildScript : BuildScriptPackedMode
{
    public override string Name => "Tracked Image Build Script";

    protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput)
    {
        Debug.Log("[TrackedImageBuildScript] Running ARBuildProcessor.PreprocessBuild() for ARCore data...");
        
        // Buộc chạy preprocess để tạo .imgdb cho XRReferenceImageLibrary
        ARBuildProcessor.PreprocessBuild(builderInput.Target);

        return base.BuildDataImplementation<TResult>(builderInput);
    }
}