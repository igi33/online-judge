#include <cstdio>
#include <algorithm>
using namespace std;

int n, i, j;
short c[2][5006];
char s[5006], rs[5006];

inline int lcs (char *s1, char *s2, int n) {
     int i, j;
     for (i = 1; i <= n; ++i) {
         for (j = 1; j <= n; ++j) {
             if (s1[i-1] == s2[j-1]) c[1][j] = c[0][j-1]+1;
             else c[1][j] = max (c[1][j-1], c[0][j]);
         }
    	 for (j = 1; j <= n; ++j) c[0][j] = c[1][j];
     }
     return (c[1][n]);
}

int main () {
    scanf ("%d\n", &n);
    for (i = 0; i < n; ++i) {
        scanf ("%c", &s[i]);
        rs[n-i-1] = s[i];
    }
    s[n] = rs[n] = '\0';
    printf ("%d\n", n - lcs (s, rs, n));
    return 0;
}
