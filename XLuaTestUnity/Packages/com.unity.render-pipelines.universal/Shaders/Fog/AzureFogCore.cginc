#include "Packages/com.unity.render-pipelines.universal/Shaders/Fog/AzureComputeFogScattering.cginc"

// Apply fog scattering.
half3 ApplyAzureFog (half4 fragOutput, float3 worldPos)
{	
	#ifdef UNITY_PASS_FORWARDADD
		half3 fogScatteringColor = 0;
	#else
		half3 fogScatteringColor = AzureComputeFogScattering(worldPos);
	#endif

	// Calcule Standard Fog.
	float depth = distance(_WorldSpaceCameraPos, worldPos);
	float fogFactor = smoothstep(-_Azure_FogBlend, 1.25, depth / _Azure_FogDistance);
	// Apply Fog.
	#if defined(_ALPHAPREMULTIPLY_ON)
	fragOutput.a = lerp(fragOutput.a, 1.0, fogFactor);
	#endif
	fogScatteringColor = lerp(fragOutput.rgb, fogScatteringColor, fogFactor * lerp(fragOutput.a, 1.0, 2.0 - fogFactor));
	return fogScatteringColor;
}

// Apply fog scattering to additive/multiply blend mode.
half3 ApplyAzureFog (float4 fragOutput, float3 worldPos, float3 fogColor)
{	
	#ifdef UNITY_PASS_FORWARDADD
		half3 fogScatteringColor = 0;
	#else
		half3 fogScatteringColor = fogColor;
	#endif	

	// Calcule Standard Fog.
	float depth = distance(_WorldSpaceCameraPos, worldPos);
	float fogFactor = smoothstep(-_Azure_FogBlend, 1.25, depth / _Azure_FogDistance);

	// Apply Fog.
	fogScatteringColor = lerp(fragOutput.rgb, fogScatteringColor, fogFactor * lerp(fragOutput.a, 1.0, 2.0 - fogFactor));
	return fogScatteringColor;
}
half4 VetApplyAzureFog (float3 worldPos)
{	
	#ifdef UNITY_PASS_FORWARDADD
		half3 fogScatteringColor = 0;
	#else
		half3 fogScatteringColor = AzureComputeFogScattering(worldPos);
	#endif

	// Calcule Standard Fog.
	float depth = distance(_WorldSpaceCameraPos, worldPos);
	float fogFactor = smoothstep(-_Azure_FogBlend, 1.25, depth / _Azure_FogDistance);
	fogFactor = fogFactor * (2.0 - fogFactor);
	//fogScatteringColor = lerp(fragOutput.rgb, fogScatteringColor, fogFactor * lerp(fragOutput.a, 1.0, 2.0 - fogFactor));
	return half4(fogScatteringColor,fogFactor);
}