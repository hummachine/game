#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
#define UNITY_4
#endif

using UnityEngine;
using System.Collections;


public class TOD_WeatherManager : MonoBehaviour
{
	public enum RainType
	{
		None,
		Light,
		Heavy
	}

	public enum CloudType
	{
		None,
		Few,
		Scattered,
		Broken,
		Overcast
	}

	public enum AtmosphereType
	{
		Clear,
		Storm,
		Dust,
		Fog
	}

	public ParticleSystem RainParticleSystem = null;

	public float FadeTime = 10f;

	public RainType       Rain       = default(RainType);
	public CloudType      Clouds     = default(CloudType);
	public AtmosphereType Atmosphere = default(AtmosphereType);

	private float cloudOpacityMax;
	private float cloudBrightnessMax;
	private float atmosphereBrightnessMax;
	private float rainEmissionMax;

	private float cloudOpacity;
	private float cloudCoverage;
	private float cloudBrightness;
	private float atmosphereFog;
	private float atmosphereBrightness;
	private float rainEmission;

	private float GetRainEmission()
	{
		if (RainParticleSystem)
		{
			#if UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
			return RainParticleSystem.emissionRate;
			#elif UNITY_5_3 || UNITY_5_4
			return RainParticleSystem.emission.rate.curveScalar;
			#else
			return RainParticleSystem.emission.rateOverTimeMultiplier;
			#endif
		}
		else
		{
			return 0;
		}
	}

	private void SetRainEmission(float value)
	{
		if (RainParticleSystem)
		{
			#if UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
			RainParticleSystem.emissionRate = value;
			#elif UNITY_5_3 || UNITY_5_4
			var emission = RainParticleSystem.emission;
			var rate = emission.rate;
			rate.curveScalar = value;
			emission.rate = rate;
			#else
			var emission = RainParticleSystem.emission;
			emission.rateOverTimeMultiplier = value;
			#endif
		}
	}

	protected void Start()
	{
		var sky = TOD_Sky.Instance;

		// Get current values
		cloudOpacity         = sky.Clouds.Opacity;
		cloudCoverage        = sky.Clouds.Coverage;
		cloudBrightness      = sky.Clouds.Brightness;
		atmosphereFog        = sky.Atmosphere.Fogginess;
		atmosphereBrightness = sky.Atmosphere.Brightness;
		rainEmission         = GetRainEmission();

		// Get maximum values
		cloudOpacityMax         = cloudOpacity;
		cloudBrightnessMax      = cloudBrightness;
		atmosphereBrightnessMax = atmosphereBrightness;
		rainEmissionMax         = rainEmission;


		StartCoroutine(CheckWeather());
	}

	protected void Update()
	{
		var sky = TOD_Sky.Instance;

		// Update rain state
		switch (Rain)
		{
			case RainType.None:
				rainEmission = 0.0f;
				break;

			case RainType.Light:
				rainEmission = rainEmissionMax * 0.5f;
                cloudBrightness = cloudBrightnessMax * 0.2f;
                atmosphereBrightness = atmosphereBrightnessMax * 0.0f;
                break;

			case RainType.Heavy:
				rainEmission = rainEmissionMax;
				break;
		}

		// Update cloud state
		switch (Clouds)
		{
			case CloudType.None:
				cloudOpacity  = 0.0f;
				cloudCoverage = 0.0f;
				break;

			case CloudType.Few:
				cloudOpacity  = cloudOpacityMax;
				cloudCoverage = 0.1f;
				break;

			case CloudType.Scattered:
				cloudOpacity  = cloudOpacityMax;
				cloudCoverage = 0.3f;
				break;

			case CloudType.Broken:
				cloudOpacity  = cloudOpacityMax;
				cloudCoverage = 0.6f;
				break;

			case CloudType.Overcast:
				cloudOpacity  = cloudOpacityMax;
				cloudCoverage = 1.0f;
				break;
		}

		// Update atmosphere state
		switch (Atmosphere)
		{
			case AtmosphereType.Clear:
				cloudBrightness      = cloudBrightnessMax;
				atmosphereBrightness = atmosphereBrightnessMax;
				atmosphereFog        = 0.0f;
				break;

			case AtmosphereType.Storm:
				cloudBrightness      = cloudBrightnessMax * 0.2f;
				atmosphereBrightness = atmosphereBrightnessMax * 0.3f;
				atmosphereFog        = 1.0f;
				break;

			case AtmosphereType.Dust:
				cloudBrightness      = cloudBrightnessMax;
				atmosphereBrightness = atmosphereBrightnessMax;
				atmosphereFog        = 0.5f;
				break;

			case AtmosphereType.Fog:
				cloudBrightness      = cloudBrightnessMax;
				atmosphereBrightness = atmosphereBrightnessMax;
				atmosphereFog        = 1.0f;
				break;
		}

		// FadeTime is not exact as the fade smoothens a little towards the end
		float t = Time.deltaTime / FadeTime;

		// Update visuals
		sky.Clouds.Opacity        = Mathf.Lerp(sky.Clouds.Opacity,        cloudOpacity,         t);
		sky.Clouds.Coverage       = Mathf.Lerp(sky.Clouds.Coverage,       cloudCoverage,        t);
		sky.Clouds.Brightness     = Mathf.Lerp(sky.Clouds.Brightness,     cloudBrightness,      t);
		sky.Atmosphere.Fogginess  = Mathf.Lerp(sky.Atmosphere.Fogginess,  atmosphereFog,        t);
		sky.Atmosphere.Brightness = Mathf.Lerp(sky.Atmosphere.Brightness, atmosphereBrightness, t);

		SetRainEmission(Mathf.Lerp(GetRainEmission(), rainEmission, t));
	}

	IEnumerator CheckWeather() {
		while (true) {
			// EDIT THIS TO LAT LON OF PLACE 
			string weatherUrl = "http://api.openweathermap.org/data/2.5/weather?lat=17.75&lon=142.5000&appid=e72fb8413ce86ecd7c649b180210f7bb";

			//{"coord":{"lon":-0.13,"lat":51.51},"weather":[{"id":721,"main":"Haze","description":"haze","icon":"50n"},{"id":701,"main":"Mist","description":"mist","icon":"50n"}],"base":"stations","main":{"temp":2.84,"pressure":1028,"humidity":86,"temp_min":1,"temp_max":5},"visibility":4000,"wind":{"speed":3.1,"deg":240,"gust":8.2},"clouds":{"all":20},"dt":1485193800,"sys":{"type":1,"id":5091,"message":0.1948,"country":"GB","sunrise":1485157806,"sunset":1485189339},"id":2643743,"name":"London","cod":200}

			WWW weatherWWW = new WWW (weatherUrl);
			yield return weatherWWW;
			Debug.Log (weatherWWW.text);
			JSONObject tempData = new JSONObject (weatherWWW.text);

			JSONObject weatherDetails = tempData ["weather"];
			string WeatherType = weatherDetails [0] ["main"].str;
			Debug.Log (WeatherType);


			string WeatherID = weatherDetails [0] ["id"].ToString ();
			Debug.Log (WeatherID);

			switch (WeatherID) {
			case "200":
			case "201":
			//200 thunderstorm with light rain     11d
			//201 thunderstorm with rain   11d
				Debug.Log ("rumble");
				Rain = RainType.Light;
				Clouds = CloudType.Overcast;
				Atmosphere = AtmosphereType.Storm;
				break;
			case "202":
			//202 thunderstorm with heavy rain     11d
				Debug.Log ("rumble");
				Rain = RainType.Heavy;
				Clouds = CloudType.Overcast;
				Atmosphere = AtmosphereType.Storm;
				break;
			case "210":
			case "211":
			//210 light thunderstorm   11d
			//211 thunderstorm     11d
				Debug.Log ("rumble");
				Rain = RainType.Light;
				Clouds = CloudType.Overcast;
				Atmosphere = AtmosphereType.Storm;
				break;
			case "212":
			case "221":
			//212 heavy thunderstorm   11d
			//221 ragged thunderstorm  11d
				Debug.Log ("rumble");
				Rain = RainType.Heavy;
				Clouds = CloudType.Overcast;
				Atmosphere = AtmosphereType.Storm;
				break;
			case "230":
			case "231":
			//230 thunderstorm with light drizzle  11d
			//231 thunderstorm with drizzle    11d
				Debug.Log ("rumble");
				Rain = RainType.Light;
				Clouds = CloudType.Scattered;
				Atmosphere = AtmosphereType.Storm;
				break;
			case "232":
			//232 thunderstorm with heavy drizzle  11d
				Debug.Log ("rumble");
				Rain = RainType.Light;
				Clouds = CloudType.Overcast;
				Atmosphere = AtmosphereType.Storm;
				break;
			case "300":
			case "301":
			case "302":
			case "310":
			case "311":
			case "312":
			case "313":
			case "314":
			case "321":
			//300 light intensity drizzle  09d
			//301 drizzle  09d
			//302 heavy intensity drizzle  09d
			//310 light intensity drizzle rain     09d
			//311 drizzle rain     09d
			//312 heavy intensity drizzle rain     09d
			//313 shower rain and drizzle  09d
			//314 heavy shower rain and drizzle    09d
			//321 shower drizzle   09d
				Debug.Log ("drizzle");
				Rain = RainType.Light;
				Clouds = CloudType.Broken;
				Atmosphere = AtmosphereType.Clear;
				break;
			case "500":
			case "501":			
			//500 light rain   10d
			//501 moderate rain    10d
				Debug.Log ("light rain");
				Rain = RainType.Light;
				Clouds = CloudType.Broken;	
				Atmosphere = AtmosphereType.Dust;
				break;
			case "502":
			case "503":
			case "504":
			case "511":
			//502 heavy intensity rain     10d
			//503 very heavy rain  10d
			//504 extreme rain     10d
			//511 freezing rain    13d
				Debug.Log ("heavy rain");
				Rain = RainType.Heavy;
				Clouds = CloudType.Broken;	
				Atmosphere = AtmosphereType.Dust;
				break;
			case "520":
			case "521":
			//520 light intensity shower rain  09d
			//521 shower rain  09d
				Debug.Log ("light showers rain");
				Rain = RainType.Light;
				Clouds = CloudType.Scattered;	
				Atmosphere = AtmosphereType.Dust;
				break;
			case "522":
			case "531":
			//522 heavy intensity shower rain  09d
			//531 ragged shower rain   09d
				Debug.Log ("heavy showers rain");
				Rain = RainType.Heavy;
				Clouds = CloudType.Scattered;	
				Atmosphere = AtmosphereType.Dust;
				break;
			case "600":
			case "601":
			case "602":
			case "611":
			case "612":
			case "615":
			case "616":
			case "620":
			case "621":
			case "622":
			//Group 6xx: Snow
			//600 light snow[[file:13d.png]]
			//601 snow[[file:13d.png]]
			//602 heavy snow[[file:13d.png]]
			//611 sleet[[file:13d.png]]
			//612 shower sleet[[file:13d.png]]
			//615 light rain and snow[[file:13d.png]]
			//616 rain and snow[[file:13d.png]]
			//620 light shower snow[[file:13d.png]]
			//621 shower snow[[file:13d.png]]
			//622 heavy shower snow[[file:13d.png]]
				Debug.Log ("snow");
				Rain = RainType.Light;
				Clouds = CloudType.Overcast;	
				Atmosphere = AtmosphereType.Fog;
				break;
			case "701":
			case "711":
			case "721":
			case "731":
			case "741":
			case "751":
			case "761":
			case "762":
			case "771":
			case "781":
			//Group 7xx: Atmosphere
			//701 mist[[file:50d.png]]
			//711 smoke[[file:50d.png]]
			//721 haze[[file:50d.png]]
			//731 sand, dust whirls[[file:50d.png]]
			//741 fog[[file:50d.png]]
			//751 sand[[file:50d.png]]
			//761 dust[[file:50d.png]]
			//762 volcanic ash[[file:50d.png]]
			//771 squalls[[file:50d.png]]
			//781 tornado[[file:50d.png]]
				Debug.Log ("atmosphere");
				Rain = RainType.None;
				Clouds = CloudType.Overcast;
				Atmosphere = AtmosphereType.Dust;
				break;
			case "800":
			//Group 800: Clear
			//800 clear sky[[file:01d.png]] [[file:01n.png]]
				Debug.Log ("clear sky");
				Rain = RainType.None;
				Clouds = CloudType.None;	
				Atmosphere = AtmosphereType.Clear;
				break;
			case "801":
			//Group 80x: Clouds
			//801	few clouds[[file:02d.png]] [[file:02n.png]]
				Debug.Log ("few clouds");
				Rain = RainType.None;
				Clouds = CloudType.Few;	
				Atmosphere = AtmosphereType.Clear;
				break;
			case "802":
			//802	scattered clouds[[file:03d.png]] [[file:03d.png]]
				Debug.Log ("scattered clouds");
				Rain = RainType.None;
				Clouds = CloudType.Scattered;	
				Atmosphere = AtmosphereType.Clear;
				break;
			case "803":
			//803	broken clouds[[file:04d.png]] [[file:03d.png]]
				Debug.Log ("broken clouds");
				Rain = RainType.None;
				Clouds = CloudType.Broken;	
				Atmosphere = AtmosphereType.Clear;
				break;
			case "804":
			//804	overcast clouds[[file:04d.png]] [[file:04d.png]]
				Debug.Log ("overcast clouds");
				Rain = RainType.None;
				Clouds = CloudType.Overcast;	
				Atmosphere = AtmosphereType.Clear;
				break;
			case "900":
			case "901":
			case "902":
			case "903":
			case "904":
			case "905":
			case "906":
			//Group 90x: Extreme
			//900	tornado
			//901	tropical storm
			//902	hurricane
			//903	cold
			//904	hot
			//905	windy
			//906	hail
				Debug.Log ("extreme");
				Rain = RainType.Heavy;
				Clouds = CloudType.Overcast;	
				Atmosphere = AtmosphereType.Storm;
				break;
			case "951":
			case "952":
			case "953":
			case "954":
			case "955":
			//Group 9xx: Additional
			//951	calm
			//952	light breeze
			//953	gentle breeze
			//954	moderate breeze
				Debug.Log ("fresh breeze");
				Rain = RainType.None;
				Clouds = CloudType.Few;	
				Atmosphere = AtmosphereType.Clear;
				break;
			case "956":
				Debug.Log ("strong breeze");
				Rain = RainType.None;
				Clouds = CloudType.Few;	
				Atmosphere = AtmosphereType.Clear;
				break;
			case "957":
				Debug.Log ("high wind");
				Rain = RainType.None;
				Clouds = CloudType.Scattered;	
				Atmosphere = AtmosphereType.Clear;
				break;
			case "958":
			case "959":
				Debug.Log ("severe gale");
				Rain = RainType.None;
				Clouds = CloudType.Broken;	
				Atmosphere = AtmosphereType.Dust;
				break;
			case "960":
			case "961":
				Debug.Log ("storm");
				Rain = RainType.Light;
				Clouds = CloudType.Overcast;	
				Atmosphere = AtmosphereType.Storm;
				break;
			case "962":
				Debug.Log ("hurrican");
				Rain = RainType.Heavy;
				Clouds = CloudType.Overcast;	
				Atmosphere = AtmosphereType.Storm;
				break;
			default:
				Debug.Log ("unexpected weather id");
				Rain = RainType.None;
				Clouds = CloudType.Scattered;	
				Atmosphere = AtmosphereType.Clear;
				break;
			}

			yield return new WaitForSeconds (120);
		}
	}
}
