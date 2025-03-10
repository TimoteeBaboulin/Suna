using UnityEngine;

public class TestShootRecoil : MonoBehaviour
{
    public TestPlayerViewRotation tpvr;
    public GameObject impactGo;

    [Range(0, 7f)] public float recoilAmountY;
    [Range(0, 3f)] public float recoilAmountX;

    public int roundPerSeconds = 1;

    public float waitTillNextFire;
    public float currentRecoilXPos;
    public float currentRecoilYPos;

    [Range(0, 10f)] public float maxRecoilTime = 4;

    public float timePressed;

    private void Update()
    {
        firing();
    }

    public void RecoilMath()
    {
        //float amplifier = 10;
        //currentRecoilXPos = ((Random.value - .5f) / 2) * recoilAmountX;
        //currentRecoilYPos = ((Random.value - .5f) / 2) * (timePressed >= maxRecoilTime ? recoilAmountY / 4 : recoilAmountY);
        //tpvr.wantedCameraXRotation -= Mathf.Abs(currentRecoilYPos) * amplifier * Time.deltaTime;
        //tpvr.wantedYRotation -= currentRecoilXPos * amplifier * Time.deltaTime;



        //float speed = 8;
        //float amplifier = 100;

        //float sine = Mathf.Sin(speed * timePressed);
        //float cosine = Mathf.Cos(speed * timePressed);

        //currentRecoilXPos = amplifier * speed * .125f * sine * sine * sine + amplifier * Mathf.Sin(speed * timePressed);
        //currentRecoilYPos = amplifier * speed * .125f * cosine * cosine * cosine + amplifier * Mathf.Sin(speed * speed * timePressed);
        //tpvr.wantedCameraXRotation -= currentRecoilYPos * Time.deltaTime;
        //tpvr.wantedYRotation -= currentRecoilXPos * Time.deltaTime;

    }

    private void firing()
    {
        if (Input.GetMouseButton(0))
        {
            fire();
            timePressed += Time.deltaTime;
            timePressed = timePressed >= maxRecoilTime ? maxRecoilTime : timePressed;
        }
        else
        {
            timePressed = 0;
        }
        waitTillNextFire -= roundPerSeconds * Time.deltaTime;
        if (waitTillNextFire < 0) waitTillNextFire = -1f;
    }

    private void fire()
    {
        if (waitTillNextFire <= 0)
        {
            RecoilMath();

            waitTillNextFire = 1;

            if (Physics.Raycast(transform.position, tpvr.theCamera.forward, out RaycastHit hit, 10f))
            {
                Instantiate(impactGo, hit.point, Quaternion.identity);
            }
        }
    }
}
