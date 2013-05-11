float Threashold : register(C0);
float4 FillColor : register(C1);

sampler2D implicitInputSampler : register(S0);

float4 main(float2 uv : TEXCOORD) : COLOR
{
	float4 color = tex2D(implicitInputSampler, uv);
	float3x3 RgbToYuv = {
		0.299,  0.587,  0.114,
		-0.169, -0.331,  0.500,
		0.500, -0.419, -0.081
	};
	float3 yuv = mul(RgbToYuv, color.rgb);

	if(yuv.x <= Threashold) {
		color = FillColor;
	} else {
		color = float4(0, 0, 0, 0);
	}
	return color;
}


