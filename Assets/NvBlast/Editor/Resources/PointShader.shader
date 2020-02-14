Shader "Unlit/PointShader"
{

	Properties{
		_Color("Color", Color) = (1,1,1)
	}

		SubShader{
		ZTest Always

		Color[_Color]
		Pass{}
	}

}