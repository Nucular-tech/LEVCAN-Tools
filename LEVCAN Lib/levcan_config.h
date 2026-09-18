/*******************************************************************************
 * LEVCAN: Light Electric Vehicle CAN protocol [LC]
 * Copyright (C) 2020 Vasiliy Sukhoparov
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 ******************************************************************************/

#include "stdafx.h"
#include "levcan_filedef.h"
#pragma unmanaged

#ifndef _LEVCAN_CONFIG_H_
#define _LEVCAN_CONFIG_H_

//user functions for critical sections
extern void lc_enable_irq(void);
extern void lc_disable_irq(void);

//Memory packing, compiler specific
#define LEVCAN_PACKED
//platform specific, define how many bytes in uint8_t
#define LEVCAN_MIN_BYTE_SIZE 1

#define LC_EXPORT __declspec(dllexport)


//Print debug messages using trace_printf
#define LEVCAN_TRACE
//You can re-define trace_printf function
#define trace_printf(format, ...) print_log("LEVCAN:" format, __VA_ARGS__)
extern int print_log(const char* format, ...);

//define to use simple file io operations
//#define LEVCAN_FILECLIENT
#define LEVCAN_FILESERVER
//define to use buffered printf
#define LEVCAN_BUFFER_FILEPRINTF
//File operations timeout for client side (ms)
#define LEVCAN_FILE_TIMEOUT 500

//define to be able to configure your device over levcan
//#define LEVCAN_PARAMETERS
#define LEVCAN_PARAMETERS_SERVER
//Float-point support for parameters
#define LEVCAN_USE_FLOAT
#define LEVCAN_USE_INT64
#define LEVCAN_USE_DOUBLE
//parameters receive buffer size
#define LEVCAN_PARAM_QUEUE_SIZE 5

//defiene to use small messages pop-ups on display
//#define LEVCAN_EVENTS

//Max own created nodes
#define LEVCAN_MAX_OWN_NODES 1
#define LEVCAN_PARAMETERS_CLIENT

//max saved nodes short names (used for search)
#define LEVCAN_MAX_TABLE_NODES 120

//Above-driver buffer size. Used to store CAN messages before calling network manager
//#define LEVCAN_TX_SIZE 60
#define LEVCAN_RX_SIZE 100
#define LEVCAN_NO_TX_QUEUE

//Default size for malloc, maximum size for static mem, data size for file i/o
#define LEVCAN_OBJECT_DATASIZE 64
#define LEVCAN_FILE_DATASIZE 512

//Enable this to use only static memory
//#define LEVCAN_MEM_STATIC

#ifdef LEVCAN_MEM_STATIC
//Maximum TX/RX objects at one time. Excl. UDP data <=8byte, this receives in fast mode
#define LEVCAN_OBJECT_SIZE 20
#else //LEVCAN_MEM_STATIC
//external malloc functions
#include <stdlib.h>
#define lcmalloc(bytes) malloc(bytes)
#define lcfree(ptr) free(ptr) 
#define lcdelay(ms) Sleep(ms)

//enable to use RTOS managed queues
#define LEVCAN_USE_RTOS_QUEUE

#ifdef LEVCAN_USE_RTOS_QUEUE

#define S1(x) #x
#define S2(x) S1(x)
#define LOCATION __FILE__ " : " S2(__LINE__)

#ifdef __cplusplus
extern "C" {
#endif

LC_EXPORT void* LC_Malloc(size_t size);
LC_EXPORT void LC_Free(void* ptr);

LC_EXPORT void* LC_QueueCreate(uint32_t length, uint32_t itemSize);
LC_EXPORT void LC_QueueDelete(void* queue);
LC_EXPORT void LC_QueueReset(void* queue);
LC_EXPORT int32_t LC_QueueSendToBack(void* queue, const void* buffer, int32_t ttwait);
LC_EXPORT int32_t LC_QueueReceive(void* queue, void* buffer, int32_t ttwait);
LC_EXPORT uint32_t LC_QueueStored(void* queue);

#ifdef __cplusplus
}
#endif

#define LC_QueueSendToBackISR(queue, item, yieldNeeded) LC_QueueSendToBack(queue, item, 0)
#define LC_QueueSendToFrontISR(queue, item, yieldNeeded) LC_QueueSendToBack(queue, item, 0)
#define LC_QueueReceiveISR(queue, item) LC_QueueReceive(queue, item, 0)

#define LC_SemaphoreCreate xSemaphoreCreateBinary
#define LC_SemaphoreDelete(sem) vSemaphoreDelete(sem)
#define LC_SemaphoreGive(sem) xSemaphoreGive(sem)
#define LC_SemaphoreGiveISR(sem, yieldNeeded) xSemaphoreGiveFromISR(sem, yieldNeeded)
#define LC_SemaphoreTake(sem, ttwait) xSemaphoreTake(sem, ttwait)

#define LC_RTOSYieldISR(yield) 
#define YieldNeeded_t uint32_t

//file operations
typedef LC_FileResult_t(CALLBACK* fOpen)(void** fileObject, char* name, LC_FileAccess_t mode);
typedef uint32_t(CALLBACK*  fTell)(void *fileObject);
typedef LC_FileResult_t(CALLBACK* fSeek)(void* fileObject, uint32_t pointer);
typedef LC_FileResult_t(CALLBACK* fRead)(void* fileObject, char* buffer, uint32_t bytesToRead, uint32_t* bytesReaded);
typedef LC_FileResult_t(CALLBACK* fWrite)(void* fileObject, char* buffer, uint32_t bytesToWrite, uint32_t* bytesWritten);
typedef LC_FileResult_t(CALLBACK* fClose)(void* fileObject);
typedef uint32_t(CALLBACK* fSize)(void* fileObject);
typedef LC_FileResult_t(CALLBACK* fTruncate)(void* fileObject);
typedef void(CALLBACK* fOnReceive)();
//extern functions
extern fOpen lcfopen;
extern fTell lcftell;
extern fSeek lcflseek;
extern fRead lcfread;
extern fWrite lcfwrite;
extern fClose lcfclose;
extern fTruncate lcftruncate;
extern fSize lcfsize;
extern fOnReceive LC_FileServerOnReceive;
#else //LEVCAN_USE_RTOS_QUEUE
#endif //no queue
#endif //mem dynamic
#endif // _LEVCAN_CONFIG_H_

#if defined(LEVCAN_SYS_OBJ_SIZ) && !defined(_LEVCAN_NODE_API_DECLARED_)
#define _LEVCAN_NODE_API_DECLARED_
#ifdef __cplusplus
extern "C" {
#endif

LC_EXPORT LC_NodeDescriptor_t* LC_Node_Create(uint8_t nodeID);
LC_EXPORT void LC_Node_Destroy(LC_NodeDescriptor_t* node);
LC_EXPORT void LC_Node_SetIdentity(LC_NodeDescriptor_t* node, const char* nodeName, const char* deviceName, const char* vendorName, uint16_t codePage, const uint32_t serial[4]);
LC_EXPORT void LC_Node_SetObjects(LC_NodeDescriptor_t* node, LC_Object_t* objects, uint16_t count);
LC_EXPORT void LC_Node_SetDirectories(LC_NodeDescriptor_t* node, void* directories, uint16_t count);
LC_EXPORT LC_NodeShortName_t LC_Node_GetShortName(LC_NodeDescriptor_t* node);
LC_EXPORT void LC_Node_SetShortName(LC_NodeDescriptor_t* node, LC_NodeShortName_t shortName);
LC_EXPORT uint8_t LC_Node_GetState(LC_NodeDescriptor_t* node);
LC_EXPORT void LC_Node_SetAccessLevel(LC_NodeDescriptor_t* node, uint8_t accessLevel);
LC_EXPORT uint8_t LC_Node_GetAccessLevel(LC_NodeDescriptor_t* node);

#ifdef __cplusplus
}
#endif
#endif

