#ifndef PARTICALS_PARAMATERS_H
#define PARTICALS_PARAMATERS_H


// DisplayParameters 显示参数
uniform float scale_min;                 
uniform float scale_max;                  // 粒子缩放
uniform float hue_variation_min;         
uniform float hue_variation_max;          // 色调旋转角度
uniform float anim_speed_min;
uniform float anim_speed_max;             // 动画速度
uniform float anim_offset_min;
uniform float anim_offset_max;            // 动画偏移
uniform float lifetime_randomness;        // 粒子生命周期
uniform vec4 color_value : source_color;  // 颜色值

// DynamicsParameters 动力学参数
uniform float initial_angle_min; 
uniform float initial_angle_max;           // 初始角度
uniform float angular_velocity_min;
uniform float angular_velocity_max;        // 角速度
uniform float initial_linear_velocity_min;
uniform float initial_linear_velocity_max; // 初始速度乘数
uniform float directional_velocity_min;
uniform float directional_velocity_max;    // 方向速度
uniform float radial_accel_min;
uniform float radial_accel_max;            // 径向速度
uniform float orbit_velocity_min;
uniform float orbit_velocity_max;          // 轨道速度


// PhysicalParameters 物理参数
uniform float linear_accel_min;          
uniform float linear_accel_max;           // 线性加速度
uniform float radial_velocity_min;
uniform float radial_velocity_max;        // 径向加速度
uniform float tangent_accel_min;
uniform float tangent_accel_max;          // 切向加速度
uniform float damping_min;
uniform float damping_max;                // 阻尼系数

uniform sampler2D alpha_curve : repeat_disable; // 透明度曲线

uniform vec3 emission_shape_offset = vec3(0.0); 
uniform vec3 emission_shape_scale = vec3(1.0);  // 发射形状的偏移和缩放

#endif