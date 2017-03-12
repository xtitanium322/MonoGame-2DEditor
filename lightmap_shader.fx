// variables
// texture sampler - world map render target loads here
sampler s0 : register(s0);

Texture2D lightsTexture;							// load render target here
sampler lightSampler = sampler_state{			// light mask texture - point lights render target sampled here
	Texture = <lightsTexture>;
};

float   intensity;								// [parameter] - intensity of ambient light (lower is darker) based on World clock
//float   radius = 100;                         // testing radius of circle (reach distance of world illuminating object)
//float   light_coords[2000];				    // space for 1000 light coordinates - testing iteration through coordinates, create corresponding array in game class
//----------------------------------------------------------------
// shader function
float4 PixelShaderFunction(float4 pos : SV_POSITION, float4 color1 : COLOR0, float2 coords : TEXCOORD0) : SV_TARGET0
{
	float4 color = tex2D(s0, coords);        // world scene texture: tiles
	float4 lightColor = tex2D(lightSampler, coords);  // color information of the lights buffer


	// limit light intensity based on world ambient light value
	lightColor.a *= 1.0f - intensity;

	// anything that doesn't have point lights is unaffected
	if (lightColor.a == 0.0f) 
		return color*intensity;

	// add color to lighting
	if (color.a != 0.0f)            
		color.rgb += (lightColor.rgb*0.65f); // last floating point number adjusts the power of color component

	// finish up
	if (intensity + lightColor.a <= 0.9f)
		return color*(intensity + lightColor.a);
	else
		return color*0.9f;
}

// technique (no need to change this function)
technique Technique1
{
	pass Pass1
	{
		PixelShader = compile ps_4_0_level_9_3 PixelShaderFunction();
	}
}

/* // latest working pixel shader
float4 PixelShaderFunction(float2 coords: TEXCOORD0) : COLOR0
{
float4 color = tex2D(s0, coords);   // world scene texture: tiles and light

float x = mouse_coords.x * 1440;  // real pixel value of mouse x
float y = mouse_coords.y * 900;   // real pixel value of mouse y
float distance = sqrt((x - (coords.x * 1440.0))*(x - (coords.x * 1440.0)) + (y - (coords.y * 900.0))*(y - (coords.y * 900.0))); // distance between all points and mouse center (in real pixels)
float rad_percent = distance / radius; // percentage of the radius

// setting alpha values
if (distance <= radius)
{
color *= 0.9f - ((0.9f - intensity)*rad_percent);
}
else
{
color *= intensity;
}

// adjustment for transparent pixels
if (color.a != 0)   // non-transparent pixels will have alpha restored to 1 to look solid
color.a = 1.0f;
// finish
return color;
}
*/


/* MULTIPLE lights  - bug: border between lights
float4 PixelShaderFunction(float2 coords: TEXCOORD0) : COLOR0
{
float4 color = tex2D(s0, coords);   // world scene texture: tiles and light
float  max_light_value = intensity; // 0.0f = darkest, 1.0f - lightest
int    lights_active = 3;

// for loop - iterate through all available light sources and find the closest one
for (int i = 0; i < 3; i++)
{
float temp_intensity = 0.0f;     // placeholder - calculate how much to multiple color of the pixel based on current light
float source_x = light_coords[i*2]*1440.0f;
float source_y = light_coords[i*2+1]*900.0f;
// calculate distance from current pixel to the light source
float distance = sqrt((source_x - (coords.x * 1440.0))*(source_x - (coords.x * 1440.0)) + (source_y - (coords.y * 900.0))*(source_y - (coords.y * 900.0))); // replace hard-coded numbers after testing
// calculate percentage of the way to the light radius edge for current light source/pixel
float rad_percent = distance / radius;
// loop
if (distance <= radius)
{
temp_intensity = 0.9f - ((0.9f - intensity)*rad_percent);
}
else
{
temp_intensity = intensity;
}

// compare to current max_light_value
if (max_light_value < temp_intensity)
max_light_value = temp_intensity;
}
// multiply color of the pixel by max light value available - based on available light sources
return (color*max_light_value);
}
*/