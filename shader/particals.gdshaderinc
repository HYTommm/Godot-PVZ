#ifndef PARTICALS_H
#define PARTICALS_H

#include "particals_paramaters_structs.gdshaderinc"

vec3 calculate_initial_position(inout DisplayParameters params, inout uint alt_seed) {
	float pi = 3.14159;
	vec3 pos = vec3(0.0);
	{ // Emission shape.
	 	pos = vec3(0.0);
	}
	return pos * emission_shape_scale + emission_shape_offset;
}

// 黑匣子函数，不要深究
vec4 rotate_hue(vec4 current_color, float hue_rot_angle) {
/**
 * 在RGB颜色空间进行色调旋转
 * 
 * @param current_color 当前RGBA颜色值
 * @param hue_rot_angle 色调旋转角度（弧度）
 * @return vec4 旋转色调后的新颜色
 * 
 * 原理：将RGB颜色分解为亮度(Y)和色度(CbCr)分量，在色度平面进行旋转，
 *       然后重新组合为RGB颜色，保持亮度不变
 */
    // 计算旋转角度的正弦和余弦值
    float hue_rot_c = cos(hue_rot_angle);
    float hue_rot_s = sin(hue_rot_angle);
    
    /**
     * 构建色调旋转矩阵（4x4，包含RGB和Alpha通道）
     * 矩阵由三个分量组成：
     * 1. 亮度保持矩阵（恒等部分）
     * 2. 色度余弦分量矩阵
     * 3. 色度正弦分量矩阵
     */
    mat4 hue_rot_mat = 
        // 分量1：亮度保持矩阵 - 保留RGB中的亮度分量
        mat4(
            vec4(0.299, 0.587, 0.114, 0.0),  // 亮度提取系数（红色通道）
            vec4(0.299, 0.587, 0.114, 0.0),  // 亮度提取系数（绿色通道）
            vec4(0.299, 0.587, 0.114, 0.0),  // 亮度提取系数（蓝色通道）
            vec4(0.000, 0.000, 0.000, 1.0)   // Alpha通道保持不变
        ) +
        
        // 分量2：色度余弦分量矩阵 - 处理色度平面上的余弦旋转
        mat4(
            vec4(0.701, -0.587, -0.114, 0.0),  // 红色通道的余弦分量
            vec4(-0.299, 0.413, -0.114, 0.0),  // 绿色通道的余弦分量
            vec4(-0.300, -0.588, 0.886, 0.0),  // 蓝色通道的余弦分量
            vec4(0.000, 0.000, 0.000, 0.0)     // Alpha通道无影响
        ) * hue_rot_c +  // 乘以余弦值
        
        // 分量3：色度正弦分量矩阵 - 处理色度平面上的正弦旋转
        mat4(
            vec4(0.168, 0.330, -0.497, 0.0),   // 红色通道的正弦分量
            vec4(-0.328, 0.035, 0.292, 0.0),   // 绿色通道的正弦分量
            vec4(1.250, -1.050, -0.203, 0.0),  // 蓝色通道的正弦分量
            vec4(0.000, 0.000, 0.000, 0.0)      // Alpha通道无影响
        ) * hue_rot_s;   // 乘以正弦值
    
    // 应用旋转矩阵并返回新颜色
    return hue_rot_mat * current_color;
}

void process_display_param(inout DisplayParameters parameters, float lifetime) {
	// Compile-time add textures.
	parameters.color.a *= texture(alpha_curve, vec2(lifetime)).r;
	parameters.color = rotate_hue(parameters.color, parameters.hue_rotation);
}

#endif