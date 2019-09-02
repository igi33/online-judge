#include <cstdio>
int main () {
    volatile int x = 1;
	volatile int y = 0;
	volatile int z = x / y;
	printf("%lf", z);
    return 0;
}
