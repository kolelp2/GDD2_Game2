Shader "Custom/VertexAdd"
{
	Subshader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		BindChannels
	{
		Bind "vertex", vertex
		Bind "color", color
	}
		Pass
	{

	}
	}
}