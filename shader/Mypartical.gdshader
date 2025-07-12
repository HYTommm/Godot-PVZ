shader_type particles;

#include "public.gdshaderinc"
#include "particals.gdshaderinc"

void start() {
	uint base_number = NUMBER;
	uint alt_seed = hash(base_number + uint(1) + RANDOM_SEED);
	DisplayParameters display_params;
	DynamicsParameters dynamics_params;
	PhysicalParameters physics_params;
	
	calculate_initial_all_params(display_params, dynamics_params, physics_params, alt_seed); // 初始化帧数据
	if (rand_from_seed(alt_seed) > AMOUNT_RATIO)	ACTIVE = false; // 如果随机数小于发射比率，则粒子隐藏
	
	
}

void process() {
	// Called every frame on existing particles (according to the Fixed FPS property).
}
