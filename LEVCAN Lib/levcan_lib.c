#include <process.h>
#include "stdafx.h"
#include "objbase.h"
#include "levcan.h"
#include "levcan_address.h"
#include "levcan_filedef.h"
#include "stdint.h"
#include <stdio.h>
#include "mq.h"  

typedef LC_Return_t(CALLBACK* sndCallback)(uint32_t header, uint32_t* data, uint8_t length);
sndCallback send;
typedef LC_Return_t(CALLBACK* fltrCallback)(uint32_t* reg, uint32_t* mask, uint8_t count);
fltrCallback filter;
LC_NodeDescriptor_t node;
HANDLE ghMutex = 0;

//avoid
LC_EXPORT void LC_Set_SendCallback(sndCallback callback) {
	send = callback;
	if (ghMutex == 0)
		ghMutex = CreateMutex(NULL, FALSE, NULL);
}

LC_EXPORT void LC_Set_FilterCallback(fltrCallback callback) {
	filter = callback;
}


LC_Return_t LC_HAL_Send(LC_HeaderPacked_t header, uint32_t* data, uint8_t length) {
	if (send != NULL)
		return (LC_Return_t)send(header.ToUint32, data, length);
	else
		return LC_ObjectError;
}

LC_Return_t LC_HAL_CreateFilterMasks(LC_HeaderPacked_t* reg, LC_HeaderPacked_t* mask, uint16_t count) {
	if (send != NULL) {
		LC_Return_t ret = 0;
		//for (int i = 0; i < count; i++)
		ret = (LC_Return_t)filter(reg, mask, count);
		return ret;
	}
	else
		return LC_ObjectError;
}

LC_Return_t LC_HAL_HalfFull(void) {
	return LC_BufferEmpty;
}

void lc_enable_irq(void) {
	ReleaseMutex(ghMutex);
}

void lc_disable_irq(void) {
	WaitForSingleObject(ghMutex, INFINITE);
}

#ifndef LEVCAN_USE_RTOS_QUEUE
LC_EXPORT void LC_Set_QueueCallbacks() {

}
#else
//QUEUE HELPERS

qCreate wrapper_QueueCreate;
qDelete wrapper_QueueDelete;
qReceive wrapper_QueueReceive;
qSendBack wrapper_QueueSendToBack;

LC_EXPORT void LC_Set_QueueCallbacks(qCreate create, qDelete delete, qReceive receive, qSendBack toback) {
	wrapper_QueueCreate = create;
	wrapper_QueueDelete = delete;
	wrapper_QueueReceive = receive;
	wrapper_QueueSendToBack = toback;
}
#endif

LC_EXPORT void LC_Set_AddressCallback(LC_RemoteNodeCallback_t address) {
	lc_addressCallback = address;
}

fOpen lcfopen;
fTell lcftell;
fSeek lcflseek;
fRead lcfread;
fWrite lcfwrite;
fClose lcfclose;
fTruncate lcftruncate;
fSize lcfsize;
fOnReceive LC_FileServerOnReceive;

LC_EXPORT void LC_Set_FileCallbacks(fOpen fopen, fTell ftell, fSeek flseek, fRead fread, fWrite fwrite, fClose fclose, fTruncate ftruncate, fSize fsize, fOnReceive onrec) {
	lcfopen = fopen;
	lcftell = ftell;
	lcflseek = flseek;
	lcfread = fread;
	lcfwrite = fwrite;
	lcfclose = fclose;
	lcftruncate = ftruncate;
	lcfsize = fsize;
	LC_FileServerOnReceive = onrec;
}

int print_log(const char* format, ...) {
	static char s_printf_buf[1024];
	va_list args;
	va_start(args, format);
	_vsnprintf(s_printf_buf, sizeof(s_printf_buf), format, args);
	va_end(args);
	OutputDebugStringA(s_printf_buf);
	return 0;
}

LC_EXPORT intptr_t* LC_LibInit() {
	LC_NodeDescriptor_t* desc = lcmalloc(sizeof(LC_NodeDescriptor_t));
	int val = LC_InitNodeDescriptor(desc);
	LC_DriverCalls_t* drv = lcmalloc(sizeof(LC_DriverCalls_t));
	drv->Filter = LC_HAL_CreateFilterMasks;
	drv->Send = LC_HAL_Send;
	drv->TxHalfFull = LC_HAL_HalfFull;
	desc->Driver = drv;
	return desc;
}