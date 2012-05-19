#ifndef _STDIO_H_
#define	_STDIO_H_

#include <_mingw.h>

#define __need_size_t
#define __need_NULL
#define __need_wchar_t
#define	__need_wint_t
#include <stddef.h>

#define	_IOREAD	1
#define	_IOWRT	2
#define	_IORW	0x0080
#define	STDIN_FILENO	0
#define	STDOUT_FILENO	1
#define	STDERR_FILENO	2
#define	EOF	(-1)
#define	FILENAME_MAX	(260)
#define FOPEN_MAX	(20)
#define TMP_MAX	32767
#define _P_tmpdir   "\\"
#define _wP_tmpdir  L"\\"
#define L_tmpnam (16)
#define _IOFBF		0x0000
#define _IOLBF		0x0040
#define _IONBF		0x0004
#define	BUFSIZ	512
#define SEEK_SET	(0)
#define	SEEK_CUR	(1)
#define SEEK_END	(2)

typedef char* va_list;

typedef struct _iobuf
{
	char*	_ptr;
	int	_cnt;
	char*	_base;
	int	_flag;
	int	_file;
	int	_charbuf;
	int	_bufsiz;
	char*	_tmpfname;
} FILE;

__MINGW_IMPORT FILE* stdin;
__MINGW_IMPORT FILE* stdout;
__MINGW_IMPORT FILE* stderr;

#define stdin	stdin
#define stdout	stdout
#define stderr	stderr

#ifdef __cplusplus
extern "C" {
#endif

FILE*	fopen (const char*, const char*);
FILE*	freopen (const char*, const char*, FILE*);
int	fflush (FILE*);
int	fclose (FILE*);
/* MS puts remove & rename (but not wide versions) in io.h  also */
int	remove (const char*);
int	rename (const char*, const char*);
FILE* tmpfile (void);
char* tmpnam (char*);
char* _tempnam (const char*, const char*);
char* tempnam (const char*, const char*);
int setvbuf (FILE*, char*, int, size_t);
void setbuf (FILE*, char*);

int	fprintf (FILE*, const char*, ...);
int	printf (const char*, ...);
int	sprintf (char*, const char*, ...);
int	_snprintf (char*, size_t, const char*, ...);
int	vfprintf (FILE*, const char*, va_list);
int	vprintf (const char*, va_list);
int	vsprintf (char*, const char*, va_list);
int	_vsnprintf (char*, size_t, const char*, va_list);

int snprintf(char* s, size_t n, const char*  format, ...);
extern inline int vsnprintf (char* s, size_t n, const char* format, va_list arg) { return _vsnprintf ( s, n, format, arg); }

/*
 * Formatted Input
 */

int	fscanf (FILE*, const char*, ...);
int	scanf (const char*, ...);
int	sscanf (const char*, const char*, ...);
/*
 * Character Input and Output Functions
 */

int	fgetc (FILE*);
char*	fgets (char*, int, FILE*);
int	fputc (int, FILE*);
int	fputs (const char*, FILE*);
int	getc (FILE*);
int	getchar (void);
char*	gets (char*);
int	putc (int, FILE*);
int	putchar (int);
int	puts (const char*);
int	ungetc (int, FILE*);
size_t	fread (void*, size_t, size_t, FILE*);
size_t	fwrite (const void*, size_t, size_t, FILE*);
int	fseek (FILE*, long, int);
long	ftell (FILE*);
void	rewind (FILE*);
typedef long long fpos_t;
int	fgetpos	(FILE*, fpos_t*);
int	fsetpos (FILE*, const fpos_t*);
void clearerr (FILE*);
int feof (FILE*);
int ferror (FILE*);
void perror (const char*);

FILE* _popen (const char*, const char*);
int _pclose (FILE*);
FILE* popen (const char*, const char*);
int pclose (FILE*);

int _flushall (void);
int _fgetchar (void);
int _fputchar (int);
FILE* _fdopen (int, const char*);
int _fileno (FILE*);
int fgetchar (void);
int fputchar (int);
FILE* fdopen (int, const char*);
int fileno (FILE*);

int fwprintf (FILE*, const wchar_t*, ...);
int wprintf (const wchar_t*, ...);
int swprintf (wchar_t*, const wchar_t*, ...);
int _snwprintf (wchar_t*, size_t, const wchar_t*, ...);
int vfwprintf (FILE*, const wchar_t*, va_list);
int vwprintf (const wchar_t*, va_list);
int vswprintf (wchar_t*, const wchar_t*, va_list);
int _vsnwprintf (wchar_t*, size_t, const wchar_t*, va_list);
int fwscanf (FILE*, const wchar_t*, ...);
int wscanf (const wchar_t*, ...);
int swscanf (const wchar_t*, const wchar_t*, ...);
wint_t fgetwc (FILE*);
wint_t fputwc (wchar_t, FILE*);
wint_t ungetwc (wchar_t, FILE*);
wchar_t* fgetws (wchar_t*, int, FILE*);
int fputws (const wchar_t*, FILE*);
wint_t getwc (FILE*);
wint_t getwchar (void);
wchar_t* _getws (wchar_t*);
wint_t putwc (wint_t, FILE*);
int _putws (const wchar_t*);
wint_t putwchar (wint_t);
FILE* _wfopen (const wchar_t*, const wchar_t*);
FILE* _wfreopen (const wchar_t*, const wchar_t*, FILE*);
FILE* _wfsopen (const wchar_t*, const wchar_t*, int);
wchar_t* _wtmpnam (wchar_t*);
wchar_t* _wtempnam (const wchar_t*, const wchar_t*);
int _wrename (const wchar_t*, const wchar_t*);
int _wremove (const wchar_t*);
void _wperror (const wchar_t*);
FILE* _wpopen (const wchar_t*, const wchar_t*);

int snwprintf(wchar_t* s, size_t n, const wchar_t*  format, ...);
extern inline int vsnwprintf (wchar_t* s, size_t n, const wchar_t* format, va_list arg) { return _vsnwprintf ( s, n, format, arg); }

FILE* wpopen (const wchar_t*, const wchar_t*);
wint_t _fgetwchar (void);
wint_t _fputwchar (wint_t);
int _getw (FILE*);
int _putw (int, FILE*);
wint_t fgetwchar (void);
wint_t fputwchar (wint_t);
int getw (FILE*);
int putw (int, FILE*);

#ifdef __cplusplus
}
#endif

#endif /* _STDIO_H_ */
