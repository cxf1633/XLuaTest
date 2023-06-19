// Scattering.
uniform float3 _Azure_Br;
uniform float3 _Azure_Bm;
uniform float  _Azure_Kr;
uniform float  _Azure_Km;
uniform float  _Azure_Scattering;
uniform float  _Azure_NightIntensity;
uniform float  _Azure_Exposure;
uniform float4 _Azure_RayleighColor;
uniform float4 _Azure_MieColor;
uniform float3 _Azure_MieG;
uniform float  _Azure_Pi316;
uniform float  _Azure_Pi14;
uniform float  _Azure_Pi;

// Deep Space.
uniform float4 _Azure_MoonBrightColor;
uniform float  _Azure_MoonBrightRange;
uniform float  _Azure_MoonEmission;
uniform float4 _Azure_MoonColor;

// Directions.
uniform float3 _Azure_SunDirection;
uniform float3 _Azure_MoonDirection;

// Options.
uniform float _Azure_LightSpeed;
uniform float _Azure_MieDepth;
uniform float _Azure_FogScale;
uniform int   _Azure_SunsetColorMode;

// Fog.
uniform float _Azure_FogDistance;
uniform float _Azure_FogBlend;
uniform float _Azure_MieDistance;

float3 AzureComputeFogScattering (float3 worldPos)
{
	//Initializations.
	//--------------------------------
	float3 inScatter = float3(0.0, 0.0, 0.0);
	float3 nightSky = float3(0.0, 0.0, 0.0);
	float3 fex = float3(0.0, 0.0, 0.0);
	float  r = length(float3(0.0, _Azure_LightSpeed, 0.0));

	//float3 vec1 = float3(0, 0.990268, 0.1391731);
	//float3 vec2 = float3(0, 495.134, 69.58655);
	//float3 vec3 = float3(0, 49.5134, 6.958655);
	float3 vec1 = float3(0, 1, 0);
	float3 vec2 = float3(0, 500, 0);
	float3 vec3 = float3(0, 50, 0);

	//Directions.
	//--------------------------------
	float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos) * -1.0;
	float  sunCosTheta = dot(viewDir, _Azure_SunDirection);
	float mieDepth = saturate(lerp(1.0, distance(_WorldSpaceCameraPos, worldPos) * _ProjectionParams.w * 4.0, _Azure_MieDistance));

	//Optical Depth.
	//--------------------------------
	//float zenith = acos(saturate(dot(vec1, viewDir)));
	float zenith = acos(saturate(dot(float3(-1.0, 1.0, -1.0), length(viewDir))));
	float z = cos(zenith) + 0.15 * pow(abs(93.885 - ((zenith * 180.0) / _Azure_Pi)), -1.253);
	float SR = _Azure_Kr / z;
	float SM = _Azure_Km / z;

	//Total Extinction.
	//--------------------------------
	fex = exp(-(_Azure_Br*SR + _Azure_Bm * SM));
	float  sunset = clamp(dot(vec1, _Azure_SunDirection), 0.0, 0.6);
	float3 extinction = lerp(fex, (1.0 - fex), sunset);

	//Scattering.
	//--------------------------------
	//float  rayPhase = 1.0 + pow(sunCosTheta, 2.0);										 //Preetham rayleigh phase function.
	float  rayPhase = 2.0 + 0.5 * pow(abs(sunCosTheta), 2.0);									 //Rayleigh phase function based on the Nielsen's paper.
	float  miePhase = _Azure_MieG.x / pow(abs(_Azure_MieG.y - _Azure_MieG.z * sunCosTheta), 1.5); //The Henyey-Greenstein phase function.

	float sunRise = saturate(dot(vec2, _Azure_SunDirection) / r);

	float3 BrTheta = _Azure_Pi316 * _Azure_Br * rayPhase * _Azure_RayleighColor.rgb * extinction;
	float3 BmTheta = _Azure_Pi14 * _Azure_Bm * miePhase * _Azure_MieColor.rgb * extinction * sunRise;
	BmTheta *= mieDepth;
	float3 BrmTheta = (BrTheta + BmTheta) / (_Azure_Br + _Azure_Bm);

	inScatter = BrmTheta * _Azure_Scattering * (1.0 - fex);
	inScatter *= sunRise;

	//Night Sky.
	//--------------------------------
	BrTheta = _Azure_Pi316 * _Azure_Br * rayPhase * _Azure_RayleighColor.rgb;
	BrmTheta = (BrTheta) / (_Azure_Br + _Azure_Bm);
	nightSky = BrmTheta * _Azure_NightIntensity * (1.0 - fex);

	// Moon Bright.
	float  moonRise = saturate(dot(vec2, _Azure_MoonDirection) / r);
	float3 moonBright = 1.0 + dot(viewDir, -_Azure_MoonDirection);
	moonBright = 1.0 / (0.25 + moonBright * _Azure_MoonBrightRange) * _Azure_MoonBrightColor.rgb;
	moonBright *= moonRise * mieDepth;

	//Output.
	//--------------------------------
	float3 fogScatteringColor = inScatter + nightSky + moonBright;

	//Tonemapping.
	fogScatteringColor = saturate(1.0 - exp(-_Azure_Exposure * fogScatteringColor));
// 
	//Color Correction.
	fogScatteringColor = pow(abs(fogScatteringColor), 2.2);
	#ifdef UNITY_COLORSPACE_GAMMA
	fogScatteringColor = pow(abs(fogScatteringColor), 0.4545);
	#else
	fogScatteringColor = fogScatteringColor;
	#endif

	return fogScatteringColor;
}