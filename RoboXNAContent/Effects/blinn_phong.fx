//-----------------------------------------------------------------------------
// Copyright (c) 2006-2008 dhpoware. All Rights Reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------
//
// This D3DX Effect file contains HLSL shaders that implement per-pixel
// Blinn-Phong and Phong lighting by evaluating Direct3D's fixed function
// lighting model per-pixel.
//
// Direct3D's fixed function lighting model is described here:
//  DirectX Documentation for C++ > DirectX Graphics > Direct3D 9 >
//  Programming Guide > Getting Started > Lights and Materials >
//  Mathematics of Lighting.
//
// All lighting calculations are performed in world space.
//
// The shaders in this effect file only support a single material and light
// source. However it should be relatively straight forward to generalize these
// shaders to support multiple lights and materials.
//
// We use a simplified version of the Direct3D fixed function spot lighting
// model in the shaders in this effect file. The simplified version produces a
// more visually appealing spotlight effect compared to a direct per-pixel
// implementation of the fixed function model.
//
// Light attenuation for the point and spot lighting models is based on a
// light radius. Light is at its brightest at the center of the sphere defined
// by the light radius. There is no lighting at the edges of this sphere.
//
//-----------------------------------------------------------------------------

struct Light
{
	float3 dir;                     // world space direction
	float3 pos;                     // world space position
	float4 ambient;
	float4 diffuse;
	float4 specular;
	float spotInnerCone;            // spot light inner cone (theta) angle
	float spotOuterCone;            // spot light outer cone (phi) angle
	float radius;                   // applies to point and spot lights only
};

struct Material
{
	float4 ambient;
	float4 diffuse;
	float4 emissive;
	float4 specular;
	float shininess;
};

//-----------------------------------------------------------------------------
// Globals.
//-----------------------------------------------------------------------------

float4x4 worldMatrix;
float4x4 worldInverseTransposeMatrix;
float4x4 worldViewProjectionMatrix;

float3 cameraPos;
float4 globalAmbient;

Light light;
Material material;

//-----------------------------------------------------------------------------
// Textures.
//-----------------------------------------------------------------------------

texture colorMapTexture;

sampler2D colorMap = sampler_state
{
	Texture = <colorMapTexture>;
    MagFilter = Linear;
    MinFilter = Anisotropic;
    MipFilter = Linear;
    MaxAnisotropy = 16;
};

//-----------------------------------------------------------------------------
// Vertex Shaders.
//-----------------------------------------------------------------------------

struct VS_INPUT
{
	float3 position : POSITION;
	float2 texCoord : TEXCOORD0;
	float3 normal : NORMAL;
};

struct VS_OUTPUT_DIR
{
	float4 position : POSITION;
	float2 texCoord : TEXCOORD0;
	float3 halfVector : TEXCOORD1;
	float3 lightDir : TEXCOORD2;
	float3 normal : TEXCOORD3;
	float4 diffuse : COLOR0;
	float4 specular : COLOR1;
};

struct VS_OUTPUT_POINT
{
	float4 position : POSITION;
	float2 texCoord : TEXCOORD0;
	float3 viewDir : TEXCOORD1;
	float3 lightDir : TEXCOORD2;
	float3 normal : TEXCOORD3;
	float4 diffuse : COLOR0;
	float4 specular : COLOR1;
};

struct VS_OUTPUT_SPOT
{
	float4 position : POSITION;
	float2 texCoord : TEXCOORD0;
	float3 viewDir : TEXCOORD1;
	float3 lightDir : TEXCOORD2;
	float3 spotDir : TEXCOORD3;
	float3 normal : TEXCOORD4;
	float4 diffuse : COLOR0;
	float4 specular : COLOR1;
};

VS_OUTPUT_DIR VS_DirLighting(VS_INPUT IN)
{
	VS_OUTPUT_DIR OUT;

	float3 worldPos = mul(float4(IN.position, 1.0f), worldMatrix).xyz;
	float3 viewDir = cameraPos - worldPos;
		
	OUT.position = mul(float4(IN.position, 1.0f), worldViewProjectionMatrix);
	OUT.texCoord = IN.texCoord;
	OUT.lightDir = -light.dir;
	OUT.halfVector = normalize(normalize(OUT.lightDir) + normalize(viewDir));
	OUT.normal = mul(IN.normal, (float3x3)worldInverseTransposeMatrix);
	OUT.diffuse = material.diffuse * light.diffuse;
	OUT.specular = material.specular * light.specular;
        
	return OUT;
}

VS_OUTPUT_POINT VS_PointLighting(VS_INPUT IN)
{
	VS_OUTPUT_POINT OUT;

	float3 worldPos = mul(float4(IN.position, 1.0f), worldMatrix).xyz;
    
	OUT.position = mul(float4(IN.position, 1.0f), worldViewProjectionMatrix);
	OUT.texCoord = IN.texCoord;
	OUT.viewDir = cameraPos - worldPos;
	OUT.lightDir = (light.pos - worldPos) / light.radius;
	OUT.normal = mul(IN.normal, (float3x3)worldInverseTransposeMatrix);
	OUT.diffuse = material.diffuse * light.diffuse;
	OUT.specular = material.specular * light.specular;
	
	return OUT;
}

VS_OUTPUT_SPOT VS_SpotLighting(VS_INPUT IN)
{
    VS_OUTPUT_SPOT OUT;
    
    float3 worldPos = mul(float4(IN.position, 1.0f), worldMatrix).xyz;
	       
    OUT.position = mul(float4(IN.position, 1.0f), worldViewProjectionMatrix);
	OUT.texCoord = IN.texCoord;
	OUT.viewDir = cameraPos - worldPos;
	OUT.lightDir = (light.pos - worldPos) / light.radius;
    OUT.spotDir = light.dir;
    OUT.normal = mul(IN.normal, (float3x3)worldInverseTransposeMatrix);
    OUT.diffuse = material.diffuse * light.diffuse;
	OUT.specular = material.specular * light.specular;
       
    return OUT;
}

//-----------------------------------------------------------------------------
// Pixel Shaders.
//-----------------------------------------------------------------------------

float4 PS_DirLighting(VS_OUTPUT_DIR IN) : COLOR
{
    float3 n = normalize(IN.normal);
    float3 h = normalize(IN.halfVector);
    float3 l = normalize(IN.lightDir);
    
    float nDotL = saturate(dot(n, l));
    float nDotH = saturate(dot(n, h));
    float power = (nDotL == 0.0f) ? 0.0f : pow(nDotH, material.shininess);   

	float4 color = (material.ambient * (globalAmbient + light.ambient)) +
                   (IN.diffuse * nDotL) + (IN.specular * power);

	return color * tex2D(colorMap, IN.texCoord);
}

float4 PS_PointLighting(VS_OUTPUT_POINT IN) : COLOR
{
    float atten = saturate(1.0f - dot(IN.lightDir, IN.lightDir));

	float3 n = normalize(IN.normal);
    float3 l = normalize(IN.lightDir);
    float3 v = normalize(IN.viewDir);
    float3 h = normalize(l + v);
    
    float nDotL = saturate(dot(n, l));
    float nDotH = saturate(dot(n, h));
    float power = (nDotL == 0.0f) ? 0.0f : pow(nDotH, material.shininess);
    
	float4 color = (material.ambient * (globalAmbient + (atten * light.ambient))) +
                   (IN.diffuse * nDotL * atten) + (IN.specular * power * atten);
                   
	return color * tex2D(colorMap, IN.texCoord);
}

float4 PS_SpotLighting(VS_OUTPUT_SPOT IN) : COLOR
{
    float atten = saturate(1.0f - dot(IN.lightDir, IN.lightDir));
    
	float3 l = normalize(IN.lightDir);
    float2 cosAngles = cos(float2(light.spotOuterCone, light.spotInnerCone) * 0.5f);
    float spotDot = dot(-l, normalize(IN.spotDir));
    float spotEffect = smoothstep(cosAngles[0], cosAngles[1], spotDot);
    
    atten *= spotEffect;
                                
    float3 n = normalize(IN.normal);
	float3 v = normalize(IN.viewDir);
	float3 h = normalize(l + v);
    
    float nDotL = saturate(dot(n, l));
    float nDotH = saturate(dot(n, h));
    float power = (nDotL == 0.0f) ? 0.0f : pow(nDotH, material.shininess);
    
    float4 color = (material.ambient * (globalAmbient + (atten * light.ambient))) +
                   (IN.diffuse * nDotL * atten) + (IN.specular * power * atten);
    
	return color * tex2D(colorMap, IN.texCoord);
}

//-----------------------------------------------------------------------------
// Techniques.
//-----------------------------------------------------------------------------

technique PerPixelDirectionalLighting
{
	pass
	{
		VertexShader = compile vs_2_0 VS_DirLighting();
		PixelShader = compile ps_2_0 PS_DirLighting();
	}
}

technique PerPixelPointLighting
{
    pass
    {
        VertexShader = compile vs_2_0 VS_PointLighting();
        PixelShader = compile ps_2_0 PS_PointLighting();
    }
}

technique PerPixelSpotLighting
{
    pass
    {
        VertexShader = compile vs_2_0 VS_SpotLighting();
        PixelShader = compile ps_2_0 PS_SpotLighting();
    }
}