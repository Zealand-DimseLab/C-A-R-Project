using UnityEngine;

public class HeadsetController : MonoBehaviour{
    [SerializeField] private Transform bikeTransform;
    [SerializeField] private Transform recenterTarget;
    [SerializeField] private Transform mainCamera;

    void Update(){
        transform.forward = bikeTransform.forward;

        // bool hasRecentered = false;
        if(OVRInput.GetDown(OVRInput.Button.One)){
            TryRecenter();
        }
        // if(OVRInput.Get(OVRInput.Button.One) && !hasRecentered){
        //     hasRecentered = true;
        // }
    }

    public void TryRecenter(){
        Vector3 cameraOffset = mainCamera.position - transform.position;

        cameraOffset.y = 0;

        transform.position = recenterTarget.position - cameraOffset;

        // float currentYaw = mainCamera.rotation.eulerAngles.y;

        // float targetYaw = recenterTarget.transform.eulerAngles.y;
        // Quaternion lookAt = Quaternion.LookRotation(recenterTarget.forward - transform.forward,Vector3.up);
        // transform.rotation = Quaternion.RotateTowards(transform.rotation,lookAt,10f * Time.deltaTime);
        // transform.Rotate(0,targetYaw - currentYaw,0);
    }
}
