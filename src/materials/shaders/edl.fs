
// 
// adapted from the EDL shader code from Christian Boucheny in cloud compare:
// https://github.com/cloudcompare/trunk/tree/master/plugins/qEDL/shaders/EDL
//

#define NEIGHBOUR_COUNT 8

uniform mat4 projectionMatrix;

uniform float screenWidth;
uniform float screenHeight;
uniform float near;
uniform float far;
uniform vec2 neighbours[NEIGHBOUR_COUNT];
uniform vec3 lightDir;
uniform float expScale;
uniform float radius;

//uniform sampler2D depthMap;
uniform sampler2D colorMap;

varying vec2 vUv;

/**
 * transform linear depth to [0,1] interval with 1 beeing closest to the camera.
 */
float ztransform(float linearDepth){
	return 1.0 - (linearDepth - near) / (far - near);
}

float expToLinear(float z){
    z = 2.0 * z - 1.0;
	float linear = (2.0 * near * far) / (far + near - z * (far - near));

	return linear;
}

// this actually only returns linear depth values if LOG_BIAS is 1.0
// lower values work out more nicely, though.
#define LOG_BIAS 0.01
float logToLinear(float z){
	return (pow((1.0 + LOG_BIAS * far), z) - 1.0) / LOG_BIAS;
}

float obscurance(float z, float dist){
	return max(0.0, z) / dist;
}

float computeObscurance(float linearDepth){
	vec4 P = vec4(0, 0, 1, -ztransform(linearDepth));
	vec2 uvRadius = radius / vec2(screenWidth, screenHeight);
	
	float sum = 0.0;
	
	for(int c = 0; c < NEIGHBOUR_COUNT; c++){
		vec2 N_rel_pos = uvRadius * neighbours[c];
		vec2 N_abs_pos = vUv + N_rel_pos;
		
		float neighbourDepth = logToLinear(texture2D(colorMap, N_abs_pos).a);
		
		if(neighbourDepth != 0.0){
			float Zn = ztransform(neighbourDepth);
			float Znp = dot( vec4( N_rel_pos, Zn, 1.0), P );
			
			sum += obscurance( Znp, 0.05 * linearDepth );
		}
	}
	
	return sum;
}

void main(){
	float linearDepth = logToLinear(texture2D(colorMap, vUv).a);
	
	float f = computeObscurance(linearDepth);
	f = exp(-expScale * f);
	
	vec4 color = texture2D(colorMap, vUv);
	if(color.a == 0.0 && f >= 1.0){
		discard;
	}
	
	gl_FragColor = vec4(color.rgb * f, 1.0);
}
