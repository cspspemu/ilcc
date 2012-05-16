int n = 3 + 1;

#define GREETINGS "Hello World!\n"

#ifndef __ILCC__
//#define puts(i) printf("%s", i)
#define puti(i) printf("%d", i)
#endif

typedef struct {
	int x, y;
	int z;
} Test;

int fib(n)
	int n;
{
	if (n < 2) return n;
	else return fib(n - 1) + fib(n - 2);
}

void testLoop() {
	int m = 0;
	int n;
	
	for (n = 0; n < 10; n++)
	{
		m = m + n;
	}
}

#define NULL ((void *)0)

void main() {
	//Test r;
	//r = {1, 2, 3};
	int start, end;
	int m = 42;
	int n = 0;
	int *z = (int *)malloc(m * sizeof(int));
	//z[1] = 1;
	//z[2] = 2;
	//z[3] = 3;
	
	puts("Start:");

	start = clock();
	for (n = 0; n < m; n++)
	{
		z[n] = fib(n);
	}

	//puti(z);
	
	for (n = 0; n < m; n++)
	{
		puts("");
		puti(z[n]);
	}
	
	end = clock();
	
	puts("");
	puts("Time:");
	puti(end - start);
	
	free(z);
	
	//puti(z[0]);
	/*
	puti(z);
	puts("");
	puti(z + 1);
	puts("");
	puts("Hello World!");
	memcpy(z, "Hello World!", 13);
	puts(z);
	*/
	/*
	for (n = 0; n < 10; n++) z[n] = fib(n);
	for (n = 0; n < 10; n++) {
		puti(z[n]);
		puts("");
	}
	*/
}