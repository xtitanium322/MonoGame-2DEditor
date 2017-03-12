sampler mainSampler : register(s0);

Texture2D maskTexture;

sampler maskSampler = sampler_state{
	Texture = <maskTexture>;
};

float4 PixelShaderFunction(float4 pos : SV_POSITION, float4 color1 : COLOR0, float2 texCoord : TEXCOORD0) : SV_TARGET0
{
	float4 color = tex2D(mainSampler, texCoord); //main color
	float4 maskColor = tex2D(maskSampler, texCoord);//maskTexture.Sample(maskSampler, texCoord.xy);

		if (maskColor.a != 0.0f) // some masking color exists - not transparent
			return color.a = 0.0f; // make UI pixel in this position invisible
		else
			return color;
}


technique Technique1
{
	pass Pass1
	{
		PixelShader = compile ps_4_0_level_9_3 PixelShaderFunction();
	}
}