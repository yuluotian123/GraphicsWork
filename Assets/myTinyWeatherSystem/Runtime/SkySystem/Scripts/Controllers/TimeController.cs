using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yu_Weather
{
    [ExecuteInEditMode]
    public class TimeController : MonoBehaviour
    {
        // Start is called before the first frame update
        public Slider slider;
        public AtmosphereLight[] lights;
        public Light sceneLight;
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            lights[0].transform.rotation = Quaternion.Euler(slider.value * 360, 0, 0);
            lights[1].transform.rotation = Quaternion.Euler((1-slider.value)*360,0,0);

            foreach(var light in lights)
            {
                if ((180.0f - light.transform.rotation.eulerAngles.x) < 0)
                    light.lightIndex = 1;
                else
                    light.lightIndex = 0;
            }
        }
    }
}
