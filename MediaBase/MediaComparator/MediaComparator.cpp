// MediaComparator.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <intrin.h>
#include <time.h>
#include <iostream>
#include <iomanip>
#include <queue>
#include <concurrent_queue.h>
#include <libpq-fe.h>
#include <thread>
#include <chrono>

#pragma comment(lib, "libpq")

using namespace std;

unsigned char *buf16;
unsigned char *buf16t;
long long *IDs;

int full_size;
int partial_size;

int const thread_count = 4;

__int64 counters[thread_count];
__int64 prev_counters[thread_count];
__int64 current_index[thread_count];
__int64 equCnt[thread_count];
__int64 isDone[thread_count];

__int64 SessionID=-1;

concurrency::concurrent_queue<ResItem*> * resQueue = new concurrency::concurrent_queue<ResItem*>();

char *conninfo;

class ResItem
{
public:
	long long session_id;
	int a;
	int b;
	int res;
	int tres;
	ResItem(long long _session_id, int _a, int _b, int _res, int _tres)
	{
		session_id = _session_id;
		a = _a;
		b = _b;
		res = _res;
		tres = _tres;
	};
};

// Reduce image to 16 horizontal lines with average values
void avg16(unsigned char *src, unsigned char *dst, int __length)
{
	int length = __length * 16;
	const __m128i zero = _mm_setzero_si128();
	__m128i a;
	__m128i b, c;
	for (int i = 0; i < length; i++)
	{
		a = _mm_loadu_si128((__m128i*)(src + 16 * i));
		b = _mm_unpacklo_epi8(a, zero);
		c = _mm_unpackhi_epi8(a, zero);
		a = _mm_adds_epu16(b, c);
		a = _mm_hadd_epi16(a, zero);
		a = _mm_hadd_epi16(a, zero);
		a = _mm_hadd_epi16(a, zero);
		dst[i] = a.m128i_u16[0] / 16;
	}
}

// Reduce image to 16 vertical rows with average values
void avg16t(unsigned char *src, unsigned char *dst, int __length)
{
	const __m128i zero = _mm_setzero_si128();
	__m128i a[8];
	__m128i b;
	__m128i unpack_lo, unpack_hi, sum;
	__m128i res, res1, res2, res3, res4;

	for (int t = 0; t < __length; t++)
	{
		for (int i = 0; i < 8; i++)
		{
			a[i] = _mm_avg_epu8(_mm_loadu_si128((__m128i *)(src + t*256 + i * 32)), _mm_loadu_si128((__m128i *)(src + t * 256 + i * 32 + 16)));
		}

		for (int i = 0; i < 4; i++)
		{
			a[i] = _mm_avg_epu8(a[i * 2], a[i * 2 + 1]);
		}

		for (int i = 0; i < 2; i++)
		{
			a[i] = _mm_avg_epu8(a[i * 2], a[i * 2 + 1]);
		}
		*((__m128i *)(dst+t*16)) = _mm_avg_epu8(a[0], a[1]);
	}
}

void DBSaveThread()
{
	PGconn     *conn;
	PGresult   *res;
	conn = PQconnectdb(conninfo);
	if (PQstatus(conn) != CONNECTION_OK)
	{
		printf("Connection to database failed: %s\n", PQerrorMessage(conn));
		return 0;
	};
	char buf[5120];
	ResItem* ri;
	for (__int64 i=0;;i++)
	{
		if (resQueue->empty())
		{
			this_thread::sleep_for(std::chrono::milliseconds(500));
			continue;
		};
		if (!resQueue->try_pop(ri)) continue;
		{
			sprintf_s(buf, "insert into comp_res(session_id, id1, id2, gs16res, gs16tres) values(%I64d,%I64d,%I64d,%d,%d);", ri->session_id, IDs[ri->a], IDs[ri->b], ri->res, ri->tres);
			delete ri;
		};
		res = PQexecParams(conn, (char*)buf, 0, NULL, NULL, NULL, NULL, 1);
		PQclear(res);
	}
}

inline int difference(__m128i a, __m128i b)
{
	__m128i c;
	c = _mm_sad_epu8(a, b);
	return c.m128i_u16[0];
}

void Compare(int thread_index)
{
	__int64 count = 1;
	__int64 prevcount = 1;
	__m128i c,d;
	int diff, diff_t;
	for (int i = thread_index; i < partial_size; i+=thread_count)
	{
		current_index[thread_index] = i;

		c = _mm_loadu_si128((__m128i*)(buf16 + 16 * i));
		for (int j = i + 1; j < full_size; j++)
		{
			counters[thread_index]++;

			diff = difference(c, _mm_loadu_si128((__m128i*)(buf16 + 16 * j)));
			if (diff < 16) {
				d = _mm_loadu_si128((__m128i*)(buf16t + 16 * i));
				diff_t = difference(d, _mm_loadu_si128((__m128i*)(buf16t + 16 * j)));
				if (diff_t < 16) {
					equCnt[thread_index]++; // 
					resQueue->push(new ResItem(SessionID, i, j, diff, diff_t));
				}
			}
		}

	}
	isDone[thread_index] = 1;
}

void CompareOne(int thread_index, long long task_id, int id_index, int max_difference)
{
	__int64 count = 1;
	__int64 prevcount = 1;
	__m128i c, d;
	int diff, diff_t;
	for (int i = id_index; i < id_index+1; i ++)
	{
		c = _mm_loadu_si128((__m128i*)(buf16 + 16 * i));
		for (int j = 0; j < full_size; j++)
		{
			counters[thread_index]++;

			diff = difference(c, _mm_loadu_si128((__m128i*)(buf16 + 16 * j)));
			if (diff < max_difference) {
				d = _mm_loadu_si128((__m128i*)(buf16t + 16 * i));
				diff_t = difference(d, _mm_loadu_si128((__m128i*)(buf16t + 16 * j)));
				if (diff_t < max_difference) {
					printf("df %i ~ %i [ %i, %i ] : %u %u\n", i, j, diff,diff_t,  IDs[i], IDs[j]);
					resQueue->push(new ResItem(task_id, 0, j, diff, diff_t));
				}
			}
		}

	}
}

int ExecSelectAndReturnRowCount(PGconn *conn, const char *sql, PGresult* &res,  int &count)
{
	res = PQexecParams(conn, sql, 0, NULL, NULL, NULL, NULL, 1);
	if (!res || PQresultStatus(res) != PGRES_TUPLES_OK) return 0;
	count = PQntuples(res);
	return 1;
}

int SelectFromResAndFillBuffers(PGresult* &res, unsigned char *_buf16, unsigned char *_buf16t, long long *_IDs, int count)
{
	int size;
	const char* contents;
	long long ID;
	unsigned char *buf;
	buf = new unsigned char[256 * count];

	for (int i = 0; i < count; i++)
	{
		size = PQgetlength(res, i, 0);
		contents = PQgetvalue(res, i, 0);
		ID = *((long long*)contents);
		_IDs[i] = _byteswap_uint64(ID);

		size = PQgetlength(res, i, 1);
		contents = PQgetvalue(res, i, 1);
		memcpy(buf + 256 * i, contents, size);
	};

	avg16(buf, _buf16, count);
	avg16t(buf, _buf16t, count);

	delete[] buf;
	return 1;
}

int OpenCompareSession()
{
	PGconn     *conn;
	PGresult   *res;

	unsigned char *buf;
	long long *newIDs;
	long long ID;
	int         nnotifies;
	const char* contents;
	int newImagesCount;
	int prevImagesCount;
	int fullImagesCount;
	int size;
	int nres;
	ExecStatusType est;
	char *data;
	char sql[1024];

	long long prev_SessionID = SessionID;

	conn = PQconnectdb(conninfo);
	if (PQstatus(conn) != CONNECTION_OK)
	{
		printf("Connection to database failed: %s\n", PQerrorMessage(conn));
		return 0;
	}
	else printf("Connected\n");

	res = PQexecParams(conn, "select * from open_comp_session();", 0, NULL, NULL, NULL, NULL, 1);
	if (!res || PQresultStatus(res) != PGRES_TUPLES_OK) return 0;
	contents = PQgetvalue(res, 0, 0);
	SessionID = *((long long*)contents);
	SessionID = _byteswap_uint64(SessionID);
	PQclear(res);
	printf("Session ID=%I64d\n", SessionID);

	sprintf_s(sql, 1024, "SELECT up, gs16x16 FROM grayscaled_img where session_id=%I64d", SessionID);
	nres = ExecSelectAndReturnRowCount(conn, sql, res, newImagesCount);
	printf("New images count=%d\n", newImagesCount);

	if (prev_SessionID < 0) // If this is a first run, let's load all previous data
	{
		PGconn     *conn2;
		PGresult   *res2;
		conn2 = PQconnectdb(conninfo);
		if (PQstatus(conn2) != CONNECTION_OK)
		{
			printf("Connection to database failed: %s\n", PQerrorMessage(conn2));
			return 0;
		}
		else printf("Connected\n");

		sprintf_s(sql, 1024, "SELECT up, gs16x16 FROM grayscaled_img where session_id<%I64d and (not_use_in_compare is null or not_use_in_compare=false)", SessionID);
		nres = ExecSelectAndReturnRowCount(conn2, sql, res2, prevImagesCount);
		printf("Previous images count=%d\n", prevImagesCount);
		
		fullImagesCount = newImagesCount + prevImagesCount;
		buf16 = new unsigned char[16 * fullImagesCount];
		buf16t = new unsigned char[16 * fullImagesCount];
		IDs = new long long[fullImagesCount];
		
		SelectFromResAndFillBuffers(res2, buf16 + 16 * newImagesCount, buf16t + 16 * newImagesCount, IDs + newImagesCount, prevImagesCount);
		PQclear(res2);
		PQfinish(conn2);
	}
	else
	{
		prevImagesCount = full_size;
		fullImagesCount = newImagesCount + full_size;
		unsigned char *prev_buf16 = buf16;
		unsigned char *prev_buf16t = buf16t;
		long long *prev_IDs = IDs;

		buf16 = new unsigned char[16 * fullImagesCount];
		buf16t = new unsigned char[16 * fullImagesCount];
		IDs = new long long[fullImagesCount];

		memcpy(buf16 + 16 * newImagesCount, prev_buf16, 16 * prevImagesCount);
		memcpy(buf16t + 16 * newImagesCount, prev_buf16t, 16 * prevImagesCount);
		memcpy(IDs + newImagesCount, prev_IDs, sizeof(long long) * prevImagesCount);

		delete[] prev_buf16;
		delete[] prev_buf16t;
		delete[] prev_IDs;
	};

	SelectFromResAndFillBuffers(res, buf16, buf16t, IDs, newImagesCount);
	PQclear(res);
	PQfinish(conn);

	partial_size = newImagesCount;
	full_size = newImagesCount + prevImagesCount;
}

int RunCompareThreadsAndMonitor()
{
	thread **compThreads = new thread*[thread_count];
	for (int i = 0; i < thread_count; i++)
	{
		counters[i] = 0; prev_counters[i] = 0; equCnt[i] = 0; isDone[i] = 0;
		compThreads[i] = new thread(Compare, i);
	};

	double start_time = clock();
	double times = clock();
	long estimateTimeToEnd;
	int hh, mi, ss;
	int Done = 0;

	__int64 Sum = 0;
	__int64 prevSum = 0;
	__int64 Tcompares = ((__int64)partial_size)*((__int64)partial_size-1) / 2 +  ((__int64)partial_size)*((__int64)full_size-partial_size);

	for (; Done<thread_count;)
	{
		Done = 0;
		double tmd = (clock() - times) / CLOCKS_PER_SEC;
		tmd = tmd == 0 ? 1 : tmd;

		Sum = 0;
		for (int i = 0; i < thread_count; i++)
		{
			cout.width(6);
			cout.fill(' ');
			cout << equCnt[i] << " ";
			cout.width(6);
			cout << fixed << setprecision(2) << ((counters[i] - prev_counters[i]) / tmd) / 1000000 << "  ";
			prev_counters[i] = counters[i];
			Sum += counters[i];
			Done += isDone[i];
		};

		cout << "   :";
		cout.width(6);
		cout << fixed << setprecision(2) << ((Sum - prevSum) / tmd) / 1000000 << "Mc/s   ";
		prevSum = Sum;

		cout << setprecision(2) << ((double)100 * Sum / Tcompares) << "%  ";// << " " << Sum << " " << Tcompares << " " << tp_count;

		estimateTimeToEnd = (Tcompares - Sum) / ((double)Sum / ((clock() - start_time) / CLOCKS_PER_SEC));
		hh = estimateTimeToEnd / 3600; estimateTimeToEnd %= 3600;
		mi = estimateTimeToEnd / 60; estimateTimeToEnd %= 60;
		cout << " Estimated: ";
		cout.width(2); cout.fill('0');
		cout << hh << ":";
		cout.width(2); cout.fill('0');
		cout << mi << ":";
		cout.width(2); cout.fill('0');
		cout << estimateTimeToEnd << "  ";
		cout << "QSz:" << resQueue->unsafe_size() << "           ";
		cout << "\r";

		times = clock();

		this_thread::sleep_for(std::chrono::milliseconds(300));
	};
	cout << "\n";
	return 1;
}

int CloseSession()
{
	PGconn     *conn;
	PGresult   *res;
	char sql[1024];

	conn = PQconnectdb(conninfo);
	if (PQstatus(conn) != CONNECTION_OK)
	{
		printf("Connection to database failed: %s\n", PQerrorMessage(conn));
		return 0;
	}
	else printf("Connected\n");

	sprintf_s(sql, 1024, "select clusterize_comparison_result(%I64d);", SessionID);
	res = PQexecParams(conn, sql, 0, NULL, NULL, NULL, NULL, 1);
	if (!res || PQresultStatus(res) != PGRES_TUPLES_OK) return 0;

	PQclear(res);
	PQfinish(conn);
	printf("Session closed\n");

	return 1;
}

long long CheckUncomparedImg()
{
	PGconn     *conn;
	PGresult   *res;
	const char* contents;
	long long count;

	conn = PQconnectdb(conninfo);
	if (PQstatus(conn) != CONNECTION_OK)
	{
		printf("Connection to database failed: %s\n", PQerrorMessage(conn));
		return 0;
	}
	else printf("Connected\n");

	res = PQexecParams(conn, "select sum(cnt) from ("
			"select count(*) as cnt from comp_session where state!=2 union " 
			"select count(*) as cnt from grayscaled_img where session_id is null) rr", 0, NULL, NULL, NULL, NULL, 1);

	if (!res || PQresultStatus(res) != PGRES_TUPLES_OK) return 0;
	contents = PQgetvalue(res, 0, 0);
	count = *((long long*)contents);
	count = _byteswap_uint64(count);
	PQclear(res);
	PQfinish(conn);

	return count;
}

int LoadFullBase(PGconn *conn, int &fullImagesCount)
{
	int newImagesCount=0;
	int prevImagesCount=0;
	int size;
	int nres;
	char sql[1024];
	PGresult   *res2;

	sprintf_s(sql, 1024, "SELECT up, gs16x16 FROM grayscaled_img");
	nres = ExecSelectAndReturnRowCount(conn, sql, res2, prevImagesCount);
	printf("Previous images count=%d\n", prevImagesCount);

	fullImagesCount = newImagesCount + prevImagesCount;
	buf16 = new unsigned char[16 * fullImagesCount];
	buf16t = new unsigned char[16 * fullImagesCount];
	IDs = new long long[fullImagesCount];

	SelectFromResAndFillBuffers(res2, buf16 + 16 * newImagesCount, buf16t + 16 * newImagesCount, IDs + newImagesCount, prevImagesCount);
	PQclear(res2);
	return 1;
}

int WaitForTaskAndLoadData(PGconn *conn, long long &taskid, long long &id, int &max_difference)
{
	int size;
	int nres;
	char sql[1024];
	char *contents;
	PGresult   *res;

	for (;;)
	{
		sprintf_s(sql, 1024, "select id, image_id, max_diff from comparison_task where state=1 limit 1");
		res = PQexecParams(conn, sql, 0, NULL, NULL, NULL, NULL, 1);
		if (!res || PQresultStatus(res) != PGRES_TUPLES_OK) return 0;
		if (PQntuples(res) > 0) break;
		this_thread::sleep_for(std::chrono::milliseconds(1000));
		PQclear(res);
	}
	contents = PQgetvalue(res, 0, 0);
	taskid = _byteswap_uint64(*((long long*)contents));
	contents = PQgetvalue(res, 0, 1);
	id = _byteswap_uint64(*((long long*)contents));
	contents = PQgetvalue(res, 0, 2);
	max_difference = _byteswap_ulong(*((int*)contents));
	PQclear(res);

	return 1;
}

int main()
{
	LoadConnectionString();

	int fullImagesCount;
	long long taskid;
	long long id; 
	int max_difference;
	int id_index;
	char sql[1024];

	thread dbsaver(DBSaveThread);
	PGresult   *res;
	PGconn *conn = PQconnectdb(conninfo);
	if (PQstatus(conn) != CONNECTION_OK)
	{
		printf("Connection to database failed: %s\n", PQerrorMessage(conn));
		return 0;
	}
	else printf("Connected\n");

	LoadFullBase(conn, fullImagesCount);
	full_size = fullImagesCount;
	partial_size = full_size;
	SessionID = 1;
	RunCompareThreadsAndMonitor();

	for (;;) this_thread::sleep_for(std::chrono::milliseconds(300));
	return 0;

}




