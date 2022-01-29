fn main() {
	s128 test = sizeof(u128);
	test *= 10;
	u128 another = addressof(u128);
	std::print(another * 10);
};