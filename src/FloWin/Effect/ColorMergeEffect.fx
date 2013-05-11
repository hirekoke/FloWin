float Key : register(C0);

sampler2D input1 : register(S0);
sampler2D input2 : register(S1);

float4 main(float2 uv : TEXCOORD) : COLOR
{
	float4 c1 = tex2D(input1, uv);
	float4 c2 = tex2D(input2, uv);

	float4 co = c1;
	
	     if(Key == 1.0) co.r = c2.r;
	else if(Key == 2.0) co.g = c2.g;
	else if(Key == 3.0) co.b = c2.b;

	return co;
}
