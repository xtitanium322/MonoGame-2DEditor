// variables
sampler s0;		// texture sampler - world map render target loads here
float4 ambient = float4(1, 1, 1, 1);
float intensity;

// The blur amount( how far away from our texel will we look up neighbour texels? )
float BlurDistance = 0.01f;

// This will use the texture bound to the object( like from the sprite batch ).
sampler ColorMapSampler : register(s0);

float4 PixelShaderFunction(float2 Tex: TEXCOORD0) : COLOR
{
	float4 Color;

	// Get the texel from ColorMapSampler using a modified texture coordinate. This
	// gets the texels at the neighbour texels and adds it to Color.
	Color = tex2D(ColorMapSampler, float2(Tex.x + BlurDistance, Tex.y + BlurDistance));
	Color += tex2D(ColorMapSampler, float2(Tex.x - BlurDistance, Tex.y - BlurDistance));
	Color += tex2D(ColorMapSampler, float2(Tex.x + BlurDistance, Tex.y - BlurDistance));
	Color += tex2D(ColorMapSampler, float2(Tex.x - BlurDistance, Tex.y + BlurDistance));
	// We need to devide the color with the amount of times we added
	// a color to it, in this case 4, to get the avg. color
	Color = Color / 4;

	// returned the blurred color
	return Color;
}

technique Technique1
{
	pass Pass1
	{
		// A post process shader only needs a pixel shader.
		PixelShader = compile ps_4_0 PixelShaderFunction(); // compiles function above
	}
}

//====================================
// color and coordinate based shaders - reference guide
//====================================
// grayscale	
/*float4 color = tex2D(s0, coords); // aka vector<float,4> color
color.gb = color.r; // assignment using properties above
return color;*/

// shadow
/*
color.grb = 0;
return color;*/

// swap colors
/*
color.grb = color.gbr;
return color;*/

// negative
/*

if (color.a)//if not trasparent
color.rgb = 1 - color.rgb;
return color;*/

// coordinate based colors
/*

if (!any(color)) return color;

float step = 1.0 / 3;

if (coords.y < (step * 1)) color = color;
else if (coords.y < (step * 2)) color = 1 - color;
else color = color;

return color;*/

// flip image 180 degrees
/*color = tex2D(s0, 1- coords);
return color;*/

//gradient
/*if (color.a)
color.rgb = coords.y;
return color;*/

//=======================
// more advanced shaders
//=======================

// ghost effect + parameter use
/*if (coords.y > param1) color = color*0.25;
else color = color*0.25;

return color;*/

// green out 

/*color.rb = 0;
color.g = color.g*0.95; // change intensity
return color;*/


