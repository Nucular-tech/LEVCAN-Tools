/*
					 * newparameters.c
					 *
					 *  Created on: 27 дек. 2020 г.
					 *      Author: VasiliSk
					 */
#include             "levcan.h"
#include             "levcan_paramserver.h"
#include             "Parameters.h"
#include             "hwconfig.h"

extern               const LCPS_Entry_t PD_Root[], PD_Controls[], PD_ControlModes[], PD_AdvancedModes[], PD_Advanced[], PD_MotorTsensor[], PD_PID[], PD_Motor[], PD_Battery[],
PD_MotorHallTable[], PD_Autoconf[], PD_DCDC[], PD_flags[], PD_Clutch[], PD_Tune[], PD_DebugInfo[], PD_Misc[], PD_PortState[], PD_PortConfig[], PD_PAS[],
PD_Debug[], PD_Menu[], PD_DebugFoc[], PD_DebugRemote[], PD_RCPWM[], PD_Cruise[], PD_Curves[];
extern               LCPS_Entry_t PD_About[], PD_Logger[];

#ifdef               ENGLISH
#define              LANG(en, ru) en
#endif
#ifdef               RUSSIAN
#define              LANG(en, ru) ru
#endif

// @formatter:on
const                LCPS_Entry_t PD_Root[] = {
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal, RD.Menu.Save, LANG("Save settings", "Сохранить настройки"), 0),
					 folder(LCP_AccessLvl_Any, 	Dir_Autoconf, 		0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_Menu, 			0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_ControlModes, 	0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_AdvancedModes, 	0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_Controls, 		0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_Motor, 			0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_Battery, 		0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_DCDC, 			0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_PortConfig, 	0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_Misc,			0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_PID, 			0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_FLAGS, 			0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_DebugInfo, 		0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_Logger, 		0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_Debug, 			0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_About, 			0, 0),
};

const                LCPS_Entry_t PD_Autoconf[] = {
					 label(LCP_AccessLvl_Any, 	0,			LANG("# Configure step-by-step! #", "# Детектить все по очереди! #"),	" "),
					 pstd(LCP_AccessLvl_Dev, 	LCP_Normal,  	RD.Configurator.BenchTest, 		((LCP_Enum_t){0, 1}), 	"Bench test", "Off\nEn\nRunning...\nOk\nFail"),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.All,									LANG("Full setup", "Полная настройка"),	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.Brake, 			((LCP_Enum_t){0, 1}),	LANG("Brake", "Ручка тормоза"),		LANG("Off\nEn\nPress\nRelease\nОК\nError" ,"Выкл\nВкл\nНажмите\nОтпустите\nОК\nОшибка")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.Throttle, 		((LCP_Enum_t){0, 1}),	LANG("Throttle", "Ручка газа"),		LANG("Off\nEn\nPress\nRelease\nОК\nError","Выкл\nВкл\nНажмите\nОтпустите\nОК\nОшибка")),
					 //	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.MotorLR, 		((LCP_Enum_t){0, 1}),	LANG("Motor LR", "Мотор LR"),		LANG("Off\nEn\nOK\nStopped","Выкл\nВкл\nОК\nОстановлен") ),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.Motor, 			((LCP_Enum_t){0, 1}),	LANG("Motor", "Мотор"),				LANG("Off\nEn\nOK\nAcceleration\nIndexing\nStopped\nHall error\nProtection\nTimeout","Выкл\nВкл\nОК\nРазгон\nИндексация\nОстановлен\nОшибка датчиков\nЗащита\nТаймаут")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.HallAjust, 		((LCP_Enum_t){0, 1}),	LANG("Angle correction", "Корректировка угла"),LANG("Off\nEn\nOK\nAcceleration\nAngle corr.\nStopped\nProtection\nTimeout","Выкл\nВкл\nОК\nРазгон\nКоррекция..\nОстановлен\nЗащита\nТаймаут")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.DetectCurrent,	((LCP_Uint32_t){2, 50, 1, 0}),	LANG("Setup current", "Ток настройки"), 	"%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.PolePairs,			((LCP_Uint32_t){1, 50, 1, 0}),	LANG("Pole pair", "Пар полюсов"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.InvertDirection, 	((LCP_Enum_t){0, 1}),	LANG("Spin direction", "Направление вращения"), LANG("Forward\nInvert", "Прямое\nОбратное")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Source, 			((LCP_Enum_t){0, 3}),	LANG("Control source", "Источник управл."),LANG("Disabled\nAuto\nEmbedd\nRemote","Отключен\nАвто\nВстроенный\nУдаленный")),
					 //	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.SensorMode, 				0, 		4, 		1, 		0),			PT_enum,LANG(,"Режим управления", "Бездатчик\nТрапеция\nСовмещенный\nFOC\nHz"  ),
					 //	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RuntimeData.Configurator.Motor, 0, 	1, 		1,		0, 	PT_uint8,		RT_bool,LANG(,"Параметры мотора",	0 ),
};

const                char preset_names[] = LANG("None\nLinear\nExponential\nNormal\nPolynomial", "Нет\nЛинейный\nЭкспонента\nНормальный\nПолином");
const                LCPS_Entry_t PD_Curves[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Menu.CurveThrottlePreset, 			((LCP_Enum_t){0, CurvePreset_MAX - 1}),	LANG("Throttle preset", "Пресет газа"), preset_names),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.ThrottleCurve[0],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Throttle start", "Газ начало"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.ThrottleCurve[1],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Throttle 1", "Газ 1"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.ThrottleCurve[2],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Throttle 2", "Газ 2"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.ThrottleCurve[3],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Throttle 3", "Газ 3"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.ThrottleCurve[4],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Throttle 4", "Газ 4"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.ThrottleCurve[5],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Throttle 5", "Газ 5"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.ThrottleCurve[6],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Throttle 6", "Газ 6"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.ThrottleCurve[7],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Throttle end", "Газ конец"), "%s%%"),

					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Menu.CurveBrakePreset, 			((LCP_Enum_t){0, CurvePreset_MAX - 1}),	LANG("Brake preset", "Пресет тормоза"), preset_names),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.BrakeCurve[0],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Brake start", "Тормоз начало"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.BrakeCurve[1],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Brake 1", "Тормоз 1"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.BrakeCurve[2],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Brake 2", "Тормоз 2"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.BrakeCurve[3],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Brake 3", "Тормоз 3"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.BrakeCurve[4],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Brake 4", "Тормоз 4"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.BrakeCurve[5],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Brake 5", "Тормоз 5"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.BrakeCurve[6],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Brake 6", "Тормоз 6"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.BrakeCurve[7],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Brake end", "Тормоз конец"), "%s%%"),
};


const                LCPS_Entry_t PD_Cruise[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Cruise.Mode, 			((LCP_Enum_t){0, 4}),	LANG("Cruise", "Круиз"), LANG("Disabled\nButton\nSwitch\nThrottle hold\nAllow Throttle hold\nMotor assist", "Отключен\nКнопка\nПереключатель\nУдержание газа\nРазрешен. удержание газа\nАссистент мотора")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Cruise.Restore, 			((LCP_Enum_t){0, 1}),	LANG("Cruise restore", "Восстанов. круиза"), LANG("Disabled\nButton", "Отключен\nКнопка")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Cruise.Delay,			((LCP_Uint32_t){ 			25, 	3000, 	25,		2}),	LANG("Cruise EN time", "Время вкл. круиза"), "%s s"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Cruise.ThrottleDelta,			((LCP_Uint32_t){ 	1, 		30, 	1,		0}),	LANG("Cruise by throttle", "Круиз по ручке газа"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Cruise.Level, 			((LCP_Enum_t){0, 2}),	LANG("Cruise level", "Уровень круиза"), LANG("Mixed\nThrottle\nSpeed", "Смешанный\nПо газу\nПо скорости")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Cruise.dERPM_ds,			((LCP_Uint32_t){ 		0, 		50000, 50,		0}),	LANG("Cruise smoothness", "Плавность круиза"), "%sERPM/s"),
					 label(LCP_AccessLvl_Any, 	0,		LANG("# Used for cruise activation:", "# Используется для вкл. круиза:"),	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Cruise.SafeAcc,			((LCP_Uint32_t){ 		100, 	10000, 	100,	0}),	LANG("Safe acceleration", "Безопасн. ускорение"), "%sERPM/s"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Cruise.MinSpeed,			((LCP_Uint32_t){ 		3, 		127, 	1,		0}),	LANG("Min. speed", "Мин. скорость"), "%skm/h"),

};

const                LCPS_Entry_t PD_PAS[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.Mode, 			((LCP_Enum_t){0, 2}),	"PAS", LANG("Disabled\nPAS sensor\nTorque sensor\nPort is busy!", "Отключен\nДатчик PAS\nДатчик давления\nПорт занят!")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.Wires, 			((LCP_Enum_t){0, 1}),	LANG("PAS connection", "Подключение"), LANG("1-wire\n2-wire", "1-провод\n2-провода")),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.Control.PAS.InvertDir, 	LANG("Invert PAS", "Инвертировать PAS"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.Poles,			((LCP_Uint32_t){ 			1, 		200, 	1,		0}),	LANG("PAS poles", "PAS полюсов"), 	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.MinFreq,			((LCP_Uint32_t){ 			1, 		500, 	1,		0}),	LANG("PAS min freq.", "PAS мин. частота"), "%s RPM"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.MaxFreq,			((LCP_Uint32_t){ 			10, 	1000, 	5,		0}),	LANG("PAS max freq.", "PAS макс. частота"), "%s RPM"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.PAS.PAS_freq_filtered,			((LCP_Uint32_t){ 	0, 		0, 		0,		0}),LANG("# PAS freq.", "# PAS частота"), "%s RPM"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.Timeout,			((LCP_Uint32_t){ 			2, 		500, 	2,		2}),	LANG("PAS timeout", "PAS таймаут"), LANG("%s sec", "%s сек")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.FilterHz,			((LCP_Uint32_t){ 			1, 		100, 	1,		0}),	LANG("PAS filter", "PAS фильтр"), "%s Hz"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.Torq_V,			((LCP_Uint32_t){ 			0, 		1000, 	5,		1}),	LANG("Pressure scale", "Шкала давления"), "%s Nm/V"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.ZeroTorq,			((LCP_Uint32_t){ 			0, 		10000, 	10,		0}),	LANG("Zero pressure", "Нулевое давление"), "%s mV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.ProtectionTorq,			((LCP_Uint32_t){ 	0, 		10000, 	100,	0}),	LANG("Protection pressure", "Уровень защиты"), "%s mV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.TorqAvg,			((LCP_Uint32_t){ 			1, 		20, 	1,		0}),	LANG("Torque averaging", "Усреднение тяги"), LANG("%s turn/2", "%s об/2")),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.PAS.PAStorque,			((LCP_Uint32_t){ 			0, 		0, 		0,		1}),LANG("# Torque", "# Момент"), "%s Nm"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.PAS.PASwatts,			((LCP_Uint32_t){ 				0, 		0, 		0,		1}),LANG("# Human watt", "# Человатт"), "%s W"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.MinWatt,			((LCP_Uint32_t){ 			0, 		500, 	10,		0}),	LANG("Human watt min", "Человатт мин."), "%s W"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.MaxWatt,			((LCP_Uint32_t){ 			0, 		1000, 	10,		0}),	LANG("Human watt max", "Человатт макс."), "%s W"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.MinTorq,			((LCP_Uint32_t){ 			0, 		100, 	2,		0}),	LANG("Torque min", "Тяга мин."), "%s Nm"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.MaxTorq,			((LCP_Uint32_t){ 			0, 		300, 	2,		0}),	LANG("Torque max", "Тяга макс."), "%s Nm"),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.Control.PAS.InstantTorq, 	LANG("Instant Torque", "Мгновенная Тяга"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.MinOutput,			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("PAS min out", "PAS мин. выход"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.MaxOutput,			((LCP_Uint32_t){ 		2, 		100, 	1,		0}),	LANG("PAS max out", "PAS макс. выход"), "%s%%"),
};

const                LCPS_Entry_t PD_RCPWM[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.RCPWM.Mode, 			((LCP_Enum_t){0, 1}),	LANG("Input P1 mode", "Режим входа P1"),LANG("Off\nPWM\nError", "Откл.\nPWM\nОшибка")),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.RCPWM.Freq,			((LCP_Uint32_t){ 				0, 		0, 		0,		1}),LANG("# Input Freq.", "# Частота входа"), "%sHZ"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.RCPWM.Period,			((LCP_Uint32_t){ 				0, 		0, 		0,		2}),LANG("# Width", "# Длина"), "%sms"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.RCPWM.Function, 			((LCP_Enum_t){0, 3}),	LANG("Function", "Функция"),LANG("Off\nThrottle\nBrake\nThrottle and brk.", "Откл.\nГаз\nТормоз\nГаз с тормозом")),
					 label(LCP_AccessLvl_Any, 	0,		LANG("# Throttle range", "# Диапазон газа"),0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.RCPWM.Throttle.Min,			((LCP_Uint32_t){ 	0, 		1000, 	1,		2}),	LANG("Throttle min", "Газ мин"),"%sms"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.RCPWM.Throttle.Max,			((LCP_Uint32_t){ 	0, 		1000, 	1,		2}),	LANG("Throttle max", "Газ макс"),"%sms"),
					 label(LCP_AccessLvl_Any, 	0,		LANG("# Brake range", "# Диапазон тормоза"),0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.RCPWM.Brake.Min,			((LCP_Uint32_t){ 		0, 		1000, 	1,		2}),	LANG("Brake min", "Тормоз мин"),"%sms"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.RCPWM.Brake.Max,			((LCP_Uint32_t){ 		0, 		1000, 	1,		2}),	LANG("Brake max", "Тормоз макс"),"%sms"),
};

enum {
	PhaseMAX = 500,
	BattIMAX = 400,
	SpeedMAX = 150,
	PowMAX = 300
};
const                LCPS_Entry_t PD_ControlModes[] = {
					 { pti(RD.Control.Motor.Ports.Speed, 		0, 		0, 		0,		0),	PT_enum | PT_readonly, LANG("# Selected mode:", "# Текущий режим:"), "N\nS1\nS2\nS3" }, //
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Torque[0],			((LCP_Uint32_t){ 				0, 	PhaseMAX, 	2,		0}),	LANG("Phase 1", "Фазный 1"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.BatteryI[0],			((LCP_Uint32_t){ 			2, 	BattIMAX, 	1,		0}),	LANG("Battery 1", "Батарейный 1"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Power[0],			((LCP_Uint32_t){ 				0, 	PowMAX, 	1,		1}),	LANG("Power 1", "Мощность 1"), "%skW"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Speed[0],			((LCP_Uint32_t){ 				4, 	SpeedMAX, 	2,		0}),	LANG("Speed 1", "Скорость 1"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Torque[1],			((LCP_Uint32_t){ 				0, 	PhaseMAX, 	2,		0}),	LANG("Phase 2", "Фазный 2"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.BatteryI[1],			((LCP_Uint32_t){ 			2, 	BattIMAX, 	1,		0}),	LANG("Battery 2", "Батарейный 2"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Power[1],			((LCP_Uint32_t){ 				0, 	PowMAX, 	1,		1}),	LANG("Power 2", "Мощность 2"), "%skW"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Speed[1],			((LCP_Uint32_t){ 				4, 	SpeedMAX, 	2,		0}),	LANG("Speed 2", "Скорость 2"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Torque[2],			((LCP_Uint32_t){ 				0, 	PhaseMAX, 	2,		0}),	LANG("Phase 3", "Фазный 3"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.BatteryI[2],			((LCP_Uint32_t){ 			2, 	BattIMAX, 	1,		0}),	LANG("Battery 3", "Батарейный 3"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Power[2],			((LCP_Uint32_t){ 				0, 	PowMAX, 	1,		1}),	LANG("Power 3", "Мощность 3"), "%skW"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Speed[2],			((LCP_Uint32_t){ 				4, 	SpeedMAX, 	2,		0}),	LANG("Speed 3", "Скорость 3"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.BrakeTorque,			((LCP_Uint32_t){ 			0, 		500, 	2,		0}),	LANG("Braking phase", "Фазный торможения"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.SpeedBrakeTorque,			((LCP_Uint32_t){ 		0, 		500, 	2,		0}),	LANG("Braking ph. at speed", "Фаз.торм. при упр.скор."), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.RevSpeed,			((LCP_Uint32_t){ 				2, 		150, 	2,		0}),	LANG("Speed reverse", "Скорость реверса"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.RevTorque,			((LCP_Uint32_t){ 				10, 	500, 	2,		0}),	LANG("Phase reverse", "Фазный реверса"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Weakening,			((LCP_Uint32_t){ 				0,	 	500, 	2,		0}),	LANG("Field weakening", "Ослабление"), "%sA"),
					 label(LCP_AccessLvl_Any, 	0,LANG("# Current change speed:", "# Скорость изменения тока:"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.dmA_dms_RisePos,			((LCP_Uint32_t){ 		50, 	50000, 	100,	0}),	LANG("- acceleration", "- разгона"), "%sA/s"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.dmA_dms_RiseNeg,			((LCP_Uint32_t){ 		50, 	50000, 	100,	0}),	LANG("- braking", "- торможения"), "%sA/s"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.dmA_dms_Fall,			((LCP_Uint32_t){ 			500, 	50000, 	100,	0}),	LANG("- shutdown", "- отключения"), "%sA/s"),
};
const                LCPS_Entry_t PD_AdvancedModes[] = {
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.AdvancedEnable, 	LANG("Enable adv. modes", "Включить доп. режимы"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.DefaultSpeedNeutral, 	LANG("Neutral by default", "Нейтраль по умолч."), 0),
					 folder(LCP_AccessLvl_Any, 	Dir_AdvancedMode1, 					0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_AdvancedMode2, 					0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_AdvancedMode3, 					0, 0),
};

const                LCPS_Entry_t PD_Advanced[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.AdvancedModes.ThrottleModes[0], 			((LCP_Enum_t){0, 2}),	LANG("Throttle mode", "Реж. ручки газа"), LANG("Speed+torque\nSpeed\nTorque" , "Скорость и тяга\nСкорость\nТяга")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.AdvancedModes.dERPM_ds_Accs[0],			((LCP_Uint32_t){ 	0, 		500000, 200,	0}),	LANG("Acceleration lim.", "Лимит ускорения"), "%sERPM/s"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.AdvancedModes.dERPM_ds_Brakes[0],			((LCP_Uint32_t){ 	0, 		500000, 200,	0}),	LANG("Deceleration lim.", "Лимит торможения"), "%sERPM/s"),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.Reverses[0], 	LANG("Reverse", "Реверс"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.Cruises[0], 	LANG("Cruise", "Круиз"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.DisableMotors[0], 	LANG("Disable motor", "Отключить мотор"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.DisableThrottles[0], 	LANG("Disable throttle", "Отключить ручку газа"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.BrakeAtEnds[0], 	LANG("Active braking", "Активное торможение"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.ReverseAtBrake[0], 	LANG("Reverse on brake", "Обратный ход при тормозе"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.SpeedControlAtZeros[0], 	LANG("Speed lim. at 0% throttle", "Лимит ск. при 0% газа"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.DisablePASs[0], 	LANG("Disable PAS", "Отключить PAS"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.AdvancedModes.PASScale[0],			((LCP_Uint32_t){ 		1, 		100, 	1,		0}),	LANG("PAS Scale", "PAS Коэфициент"), "%s%%"),
};

const                LCPS_Entry_t PD_PID[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.Square_Ki,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	5,		4}),	"Square Ki", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.Square_Kp,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	10,		3}),	"Square Kp", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.FOC_Ki,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	5,		1}),	"FOC Ki", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.FOC_Kp,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	5,		3}),	"FOC Kp", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.FOCfw_Ki,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	5,		1}),	"FW Ki", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.FOCfw_Kp,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	5,		3}),	"FW Kp", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.DCv_Ki,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	10,		0}),	"DCv Ki", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.DCv_Kp,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	2,		1}),	"DCv Kp", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.DCi_Ki,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	10,		0}),	"DCi Ki", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.DCi_Kp,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	2,		1}),	"DCi Kp", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.DCw_Ki,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	1,		1}),	"DCw Ki", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.DCw_Kp,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	1,		3}),	"DCw Kp", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.Speed_Kp,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	2,		4}),	"Speed Kp", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.Speed_Ki,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	2,		4}),	"Speed Ki", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.Speed_Kd,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	1,		6}),	"Speed Kd", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.PLL_Kp,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	20,		0}),	"PLL Kp", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.PLL_Ki,			((LCP_Uint32_t){ 			0, 	INT32_MAX, 	100,	0}),	"PLL Ki", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.Acceleration_Kp,			((LCP_Uint32_t){ 	0, 	INT32_MAX, 	2,		5}),	"Acceleration Kp", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.Acceleration_Ki,			((LCP_Uint32_t){ 	0, 	INT32_MAX, 	2,		4}),	"Acceleration Ki", 0),
					 //	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Regulators.Acceleration_Kd,			((LCP_Uint32_t){ 	0, 	INT32_MAX, 	1,		6}),	"Acceleration Kd", 0  ),
};

const                LCPS_Entry_t PD_Motor[] = {
					 folder(LCP_AccessLvl_Any, 	Dir_MotorTsensor, 					0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_Clutch, 					0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_Tune, 					0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_MotorHallTable, 					0, 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.PolePairs,			((LCP_Uint32_t){				1, 		50, 	1, 		0}),	LANG("Pole pair", "Пар полюсов"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.InvertDirection, 			((LCP_Enum_t){0, 1}),	LANG("Spin direction", "Направление вращения"), LANG("Forward\nInvert", "Прямое\nОбратное")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.IntegrationThreshold,			((LCP_Uint32_t){	0, 	INT32_MAX, 100,		3}),	LANG("Integration thr.", "Порог интегрирования"), "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.SensorModePending, 			((LCP_Enum_t){0, 5}),	LANG("Control mode now", "Реж.упр.текущ."), LANG("Sensorless\nSquare\nCombined\nFOC\nHz\nSine HZ", "Бездатчик\nТрапеция\nСовмещенный\nFOC\nHz\nSine HZ")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.SensorMode, 			((LCP_Enum_t){0, 5}),	LANG("Control mode", "Режим управления"), LANG("Sensorless\nSquare\nCombined\nFOC\nHz\nSine HZ", "Бездатчик\nТрапеция\nСовмещенный\nFOC\nHz\nSine HZ")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.CombinedTransition,			((LCP_Uint32_t){ 		0, 		200, 	5,		2}),	LANG("From hall to s-less", "Холлы->Бездатчик"), "%s rad/s"),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.PositionSensor.Interpolation, 	LANG("Interpolate halls", "Интерполяция холлов"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.InterpolationStart,			((LCP_Uint32_t){ 0, 	100, 	1,		0}),	LANG("Interpolation start", "Начало интерполяции"), "%s rad/s"),
					 //	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.MaxFreq,			((LCP_Uint32_t){ 				15000, 	25000, 	1000,	0}),LANG(,"Макс. частота"), "%sHZ"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.VFD,			((LCP_Uint32_t){ 								10, 	200, 	1,		0}),	LANG("Frequency control", "Частотное управление"), "%sHZ"),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.Motor.SquareBoost, 	LANG("Boost square current", "Усилитель тока трапеции"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.BoostCurrent,			((LCP_Uint32_t){ 			0, 		100, 	1,		0}),	LANG("Boost current", "Ток усиления"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.BoostSpeed,			((LCP_Uint32_t){ 				0, 		200, 	5,		2}),	LANG("Boost speed", "Скорость усиления"), "%s rad/s"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.MaxV,			((LCP_Uint32_t){ 					0, 		100, 	1,		0}),	LANG("Max motor U", "Макс напряжение мотора"), "%sV"),
					 { pti(Config.Motor.kV, 						0, 		INT32_MAX, 	0,	1),			PT_value ,	"kV", "%seRPM/V"}, //
					 //	{ pti(Config.Motor.Rph, 					0, 		INT32_MAX, 	0,	4),			PT_value ,	"R phase", "%s Ohm"}, //
					 //	{ pti(Config.Motor.Ld, 						0, 		INT32_MAX, 	0,	1),			PT_value ,	"Ld", "%s uH"}, //
					 //	{ pti(Config.Motor.Lq, 						0, 		INT32_MAX, 	0,	1),			PT_value ,	"Lq", "%s uH"}, //
};

const                LCPS_Entry_t PD_Tune[] = {
	//	pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.Iabs_filter,			((LCP_Uint32_t){ 				0, 		0, 		0, 		3}),LANG("# Phase amps", "# Фазн. ток"), "%sA"),
	//	pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.DCBus.Ibus_filter,			((LCP_Uint32_t){ 				0, 		0, 		0, 		3}),LANG("# Battery amps", "# Бат. ток"), "%sA"),
	//	pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.DCBus.Watts,			((LCP_Uint32_t){ 						0, 		0, 		0, 		0}),LANG("# Power", "# Мощность"), "%sW"),
	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.SquareOffset,			((LCP_Uint32_t){ 	-30, 	30, 	1, 		0}),	LANG("Offset for square", "Сдвиг для трапеции"), "%s°"),
	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.HallManualOffsetFW,			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset total fwd", "Сдвиг общий прямой"), "%s°"),
	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.HallManualOffsetRV,			((LCP_Uint32_t){	-60, 	60, 	1, 		0}),	LANG("Offset total bkwd", "Сдвиг общий обратный"), "%s°"),
	pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Configurator.ResetOffsets, 	LANG("Reset angles", "Сброс углов"), 0),
	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.IntegrationThreshold,			((LCP_Uint32_t){	0, 	INT32_MAX, 100,		3}),	LANG("Integration threshold", "Порог интегрирования"), "%sV"),
	//	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.SensorModePending, 			((LCP_Enum_t){0, 4}),	LANG(,"Реж.упр.текущ."), "Бездатчик\nТрапеция\nСовмещенный\nFOC\nHz"  ),
	//	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.Motor, 			((LCP_Enum_t){0, 1}),	LANG(,"Мотор"),LANG(,"Выкл\nВкл\nРазгон..\nИщем комбинацию\nОК\nОшибка\nОшибка датчиков" ),
	//	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.HallAjust, 			((LCP_Enum_t){0, 1}),	LANG(,"Корректировка угла"),LANG(,"Выкл\nВкл\nИщем комбинацию\nИщем угол\nРазгон..\nОК\nОшибка"  ),
	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.HallAjusterKi,			((LCP_Uint32_t){ 		2, 		500, 	2,		2}),	LANG("Hall adjust Ki", "Коэф. корректировки"), 0),
};

const                LCPS_Entry_t PD_MotorTsensor[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.TMax,			((LCP_Uint32_t){ 					500, 	2000, 	10, 	1}),	LANG("°t max", "°t макс."), "%s °C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.THysteresis,			((LCP_Uint32_t){	 			10, 	1000, 	10,  	1}),	LANG("Delta °t", "Дельта °t"), "%s °C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.TSensType, 			((LCP_Enum_t){0, PT1000}),	LANG("Sensor type", "Тип датчика"), "OFF\nRAW\nTMP35\nTMP36\nTMP37\nKTY81(82)\nKTY83\nKTY84\nNTC10k 3950\nNTC10k 3380\nPT1000"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.Temp,			((LCP_Uint32_t){ 						0, 		6, 		1, 		1}), LANG("# Value °t #", "## Значение °t ##"), "%s °C"),
};

const                LCPS_Entry_t PD_MotorHallTable[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.PosSensor.HallInput,			((LCP_Uint32_t){ 				0, 		6, 		1, 		0}), LANG("# Hall input", "# Hall вход"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.StateCommutation,			((LCP_Uint32_t){ 					0, 		6, 		1, 		0}), LANG("# Step", "# Коммутация"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.SquareOffset,			((LCP_Uint32_t){ 	-30, 	30, 	1, 		0}),	LANG("Square offset", "Сдвиг для трапеции"), "%s°"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallSeq[0],			((LCP_Uint32_t){ 	0, 		6, 		1, 		0}),	"Hall 0", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallSeq[1],			((LCP_Uint32_t){ 	0,		6, 		1, 		0}),	"Hall 1", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallSeq[2],			((LCP_Uint32_t){ 	0,		6,		1, 		0}),	"Hall 2", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallSeq[3],			((LCP_Uint32_t){  	0,		6, 		1, 		0}),	"Hall 3", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallSeq[4],			((LCP_Uint32_t){ 	0,		6, 		1, 		0}),	"Hall 4", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallSeq[5],			((LCP_Uint32_t){ 	0,		6, 		1, 		0}),	"Hall 5", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallSeq[6],			((LCP_Uint32_t){ 	0,		6, 		1, 		0}),	"Hall 6", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallSeq[7],			((LCP_Uint32_t){  	0,		6, 		1, 		0}),	"Hall 7", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[0],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset fwd 1", "Сдвиг прямой 1"), "%s°"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[1],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset fwd 2", "Сдвиг прямой 2"), "%s°"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[2],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset fwd 3", "Сдвиг прямой 3"), "%s°"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[3],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset fwd 4", "Сдвиг прямой 4"), "%s°"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[4],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset fwd 5", "Сдвиг прямой 5"), "%s°"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[5],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset fwd 6", "Сдвиг прямой 6"), "%s°"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[6],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset bkwd 1", "Сдвиг обратный 1"), "%s°"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[7],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset bkwd 2", "Сдвиг обратный 2"), "%s°"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[8],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset bkwd 3", "Сдвиг обратный 3"), "%s°"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[9],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset bkwd 4", "Сдвиг обратный 4"), "%s°"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[10],			((LCP_Uint32_t){ -60, 	60, 	1, 		0}),	LANG("Offset bkwd 5", "Сдвиг обратный 5"), "%s°"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[11],			((LCP_Uint32_t){ -60, 	60, 	1, 		0}),	LANG("Offset bkwd 6", "Сдвиг обратный 6"), "%s°"),
};

const                LCPS_Entry_t PD_Clutch[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.Mode, 			((LCP_Enum_t){0, 2}),	LANG("Mode", "Режим"),LANG("OFF\nAccelerate\nAccelerate and hold", "Откл.\nРазгон\nРазгон и удержание")),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.Iabs_filter,			((LCP_Uint32_t){ 				0, 		0, 		0, 		3}),LANG("# Phase amps", "# Фазн. ток"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.StartTime,			((LCP_Uint32_t){ 				1, 		20, 	1,		0}),	LANG("Start time", "Время пуска"),"%ss"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.StartCurrent,			((LCP_Uint32_t){ 			2, 		500, 	2,		1}),	LANG("Start current", "Ток пуска"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.DetectTime_mS,			((LCP_Uint32_t){ 			10, 	1000, 	10,		0}),	LANG("Detection time", "Время тока"),"%sms"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.VoltageRise,			((LCP_Uint32_t){ 			2, 		1000, 	2,		0}),	LANG("Acceleration", "Скорость разгона"), "%sV/s"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.Hold.CurrentLow,			((LCP_Uint32_t){ 		2, 		500, 	2,		1}),	LANG("Hold (20%)", "Ток удержания (20%)"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.Hold.CurrentHigh,			((LCP_Uint32_t){ 		2, 		500, 	2,		1}),	LANG("Hold (80%)", "Ток удержания (80%)"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.Hold.StartTime,			((LCP_Uint32_t){ 		1, 		120, 	4,		0}),	LANG("Hold enable time", "Время вкл. удержания"), "%ss"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.Hold.Time,			((LCP_Uint32_t){ 				1, 		120, 	1,		0}),	LANG("Hold time", "Длительность удержания"), "%ss"),
};

const                LCPS_Entry_t PD_Battery[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Battery.ChargedV,			((LCP_Uint32_t){	 			0, 		1000,	10, 	2}),	LANG("Full charge", "Полный заряд"), "%sdV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Battery.MaxV,			((LCP_Uint32_t){ 					2000,	9500, 	10, 	2}),	LANG("Supply max", "Питание макс"), "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Battery.MinV,			((LCP_Uint32_t){					1500, 	8000, 	10, 	2}),	LANG("Supply min", "Питание мин"), "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Battery.MaxChrgA,			((LCP_Uint32_t){ 				10,		4000, 	5, 		1}),	LANG("Charge max", "Заряд макс"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Battery.MaxDscgA,			((LCP_Uint32_t){				10, 	4000, 	5, 		1}),	LANG("Discharge max", "Разряд макс"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Battery.MaxPower,			((LCP_Uint32_t){				0,	 	30000, 	100, 	0}),	LANG("Power max", "Мощность макс"), "%sW"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.DCBus.Vbus_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0,		1}),	LANG("# DC voltage", "# DC напряжение"), "%sV"),
};

const                LCPS_Entry_t PD_DCDC[] = {
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.DC_DC.Enable, 	LANG("Enable", "Включить"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.DC_DC.Detect, 	LANG("Auto-Enable", "Авто-включение"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.DC_DC.DetectPsuV,			((LCP_Uint32_t){ 				10, 	80, 	1,  	0}),	LANG("Detection threshold", "Уровень детекта"), "%sVph"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.DC_DC.MaxBattA,			((LCP_Uint32_t){	 			0,		1000, 	5,  	1}),	LANG("Battery max I", "Батарея макс ток"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.DC_DC.MinBattA,			((LCP_Uint32_t){ 				5, 		100, 	5,  	1}),	LANG("Battery min I", "Батарея мин ток"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.DC_DC.UnderchargeV,			((LCP_Uint32_t){ 			0, 		1000, 	10,  	2}),	LANG("Undercharge", "Недозаряд"), "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.DC_DC.RampDelta,			((LCP_Uint32_t){ 				0, 		200, 	5,  	1}),	LANG("Current drop delta", "Дельта снижения тока"), "%sdV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.DC_DC.MaxPsuA,			((LCP_Uint32_t){ 				20, 	1500, 	5,  	1}),	LANG("Supply max I", "БП макс ток"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.DC_DC.DropV,			((LCP_Uint32_t){ 					50, 	1000, 	25, 	2}),	LANG("Supply drop U", "Падение U БП"), "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.DC_DC.MaxMotorT,			((LCP_Uint32_t){ 				50, 	120, 	5,  	0}),	LANG("Max motor t°", "Макс t° мотора"), "%s°С"),
					 //	{ pti(Config.DC_DC.Phases, 					0, 		2, 		1, 		0),			RT_enum,	LANG(,"Зарядн. фаз"), "Авто/nОдын/nДве"}, //
					 //	{ pti(Config.DC_DC.Inductor, 				0, 		2, 		1, 		0),			RT_enum,	LANG(,"Индуктивность"), "Авто\nМотор\nДроссель"  }, //
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.DCBus.Ibus_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0, 		1}), LANG("# Battery I", "# Ток батареи"), "%sА"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.DCBus.Vbus_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0, 		1}), LANG("# Battery U", "# Напряжение батареи"), "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.Iabs_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0, 		1}), LANG("# Supply I", "# Ток зарядки"), "%sА"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.Vabs_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0, 		1}), LANG("# Supply U", "# Напряжение зарядки"), "%sV"),
};

const                LCPS_Entry_t PD_flags[] = {
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.ResetFlags, 	LANG("Reset?", "Сбросить?"),	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Flags.Acceleration,			((LCP_Uint32_t){ 				0, 		0, 		0,		0}), LANG("Max acceleration", "Макс. ускорение"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Flags.Deceleration,			((LCP_Uint32_t){ 				0, 		0, 		0,		0}), LANG("Max deceleration", "Макс. замедление"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Flags.LastCurrent,			((LCP_Uint32_t){ 				0, 		1, 		1,		0}),	LANG("Overload current", "Ток перегрузки"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.Overcurrent, 	LANG("Overload", "Перегрузка тока"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.Overweakening, 	LANG("Over-Field weakening", "Чрезмерное ослабление"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.VBusOV, 	LANG("Supply overvoltage", "Превышение U питания"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.VBusUV, 	LANG("Supply undervoltage", "Низкое U питания"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.V12Err, 	LANG("12V protection", "Защита 12V"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.BrakeErr, 	LANG("Brake error", "Ошибка тормоза"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.ThrottleErr, 	LANG("Throtle error", "Ошибка газа"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.HallsErr, 	LANG("Hall error", "Ошибка холлов"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.CommutationErr, 	LANG("Code error", "Ошибка кода"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.PASErr, 	LANG("PAS protection", "Защита по PAS"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.TempFETErr, 	LANG("Controller overheat", "Перегрев контроллера"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.TempMotorErr, 	LANG("Motor overheat", "Перегрев мотора"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.ProtectionFail, 	LANG("Protection fail", "Неисправность защиты"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.PhaseVoltage, 	LANG("Voltage on phases", "Напряжение на фазах"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.CANErr, 	LANG("CAN: error", "CAN: ошибка"),	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Flags.CAN_LEC, 			((LCP_Enum_t){0, 7}),	"LEC",	"Ok\nStuff\nForm\nAcknowledgment\nBit recessive\nBit dominant\nCRC\nSW"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Flags.CAN_REC,			((LCP_Uint32_t){ 					0, 		255, 	1,		0}),	LANG("Receive w/error", "Получено с ошибкой"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Flags.CAN_TEC,			((LCP_Uint32_t){ 					0, 		255, 	1,		0}),	LANG("Sent w/error", "Отправлено с ошибкой"),	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Flags.CAN_OVR, 			((LCP_Enum_t){0, 3}),	LANG("CAN state ", "CAN сост."), "Ok\nOVR0\nOVR1\nOVR01"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Flags.CAN_RX,			((LCP_Uint32_t){ 						0, 		0, 		0,		0}),	"CAN  RX",	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Flags.CAN_TX,			((LCP_Uint32_t){ 						0, 		0, 		0,		0}),	"CAN TX",	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Flags.USB_RX,			((LCP_Uint32_t){ 						0, 		0, 		0,		0}),	"USB RX",	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Flags.USB_TX,			((LCP_Uint32_t){ 						0, 		0, 		0,		0}),	"USB TX",	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.CPU_Usage,			((LCP_Uint32_t){ 						0,		0,		1, 		0}),	"CPU Load", "%s%%"),
};

const                LCPS_Entry_t PD_DebugInfo[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.System.TempFET,			((LCP_Uint32_t){					0, 		0, 		0,		1}),	LANG("Temp FET", "Температура FET"), "%s °C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.Temp,			((LCP_Uint32_t){						0, 		0, 		0,		1}),	LANG("Temp Motor", "Температура мотора"), "%s °C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.DCBus.Ibus_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0,		1}),	LANG("DC current", "DC ток"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.DCBus.Vbus_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0,		1}),	LANG("DC voltage", "DC напряжение"), "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.Iabs_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0,		1}),	LANG("AC current", "AC ток"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.Vabs_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0,		1}),	LANG("AC voltage", "AC напряжение"), "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.V0_filter,			((LCP_Uint32_t){ 					0, 		0, 		0,		1}),	"Motor U0", "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.System.V12bus,			((LCP_Uint32_t){						0, 		0, 		0,		3}),	"System 12V", "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.System.V5bus,			((LCP_Uint32_t){						0, 		0, 		0,		3}),	"System 5V", "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.SpeedRPM,			((LCP_Uint32_t){					0, 		0, 		0,		0}),	"RPM", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.SpeedERPM,			((LCP_Uint32_t){					0, 		0, 		0,		0}),	"ERPM", 0),
					 { pti(RD.PosSensor.HallInput, 				0, 		0, 		0,		0),	PT_enum | PT_readonly,	LANG("Hall input", "Hall вход"), "000\n001\n010\n011\n100\n101\n110\n111" },//
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.PosSensor.HallIndex,			((LCP_Uint32_t){ 				0, 		0, 		0,		0}),	LANG("Hall index", "Hall индекс"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.Motor.ThrottleFactor,			((LCP_Uint32_t){ 		0, 		0, 		0,		3}),	LANG("Throttle %", "Газ %"),	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.Motor.BrakeFactor,			((LCP_Uint32_t){			0, 		0, 		0,		3}),	LANG("Brake %", "Тормоз %"), 	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.Motor.SpeedRequest,			((LCP_Uint32_t){ 		0, 		0, 		0,		0}),	LANG("Speed request", "Запрос скорости"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.Motor.Itorque_request,			((LCP_Uint32_t){		0, 		0, 		0,		1}),	LANG("Torque request", "Запрос тока"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.System.AmpLimAbs,			((LCP_Uint32_t){ 					0, 		0, 		0,		1}),	LANG("Torque limit", "Абс. лимит тока"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.System.TempCPU,			((LCP_Uint32_t){					0, 		0, 		0,		1}),	LANG("Temp CPU", "Температура CPU"), "%s °C"),
					 folder(LCP_AccessLvl_Any, 	Dir_DebugFOC, 					0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_DebugRemote, 					0, 0),
};

const                LCPS_Entry_t PD_DebugFoc[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.FOC.vq,			((LCP_Uint32_t){ 							0, 		0, 		0,		1}),	"U Q", "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.FOC.vd,			((LCP_Uint32_t){ 							0, 		0, 		0,		1}),	"U D", "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.FOC.iq,			((LCP_Uint32_t){ 							0, 		0, 		0,		1}),	"I Q", "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.FOC.id,			((LCP_Uint32_t){ 							0, 		0, 		0,		1}),	"I D", "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.FOC.iq_req,			((LCP_Uint32_t){ 						0, 		0, 		0,		1}),	"Ireqest Q", "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.FOC.id_req,			((LCP_Uint32_t){ 						0, 		0, 		0,		1}),	"Ireqest D", "%sA"),
};

const                LCPS_Entry_t PD_DebugRemote[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.Remote.VThrottle,			((LCP_Uint32_t){ 			0, 		0, 		0,		3}),	"Throttle", "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.Remote.VBrake,			((LCP_Uint32_t){ 			0, 		0, 		0,		3}),	"Brake", "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.Remote.ThrottleFactor,			((LCP_Uint32_t){ 	0, 		0, 		0,		2}),	"Throttle %", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.Remote.BrakeFactor,			((LCP_Uint32_t){ 		0, 		0, 		0,		2}),	"Brake %", 0),
};

#ifdef               DEBUG
const                LCPS_Entry_t PD_Debug[] = {
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Debug.LEVCANreqtest, 			"LEVCAN request test",	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Debug.LEVCANreq_sent,			((LCP_Uint32_t){ 				0, 		1, 		1,		0}),	"LEVCAN sent",	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Debug.LEVCANreq_receive,			((LCP_Uint32_t){ 			0, 		1, 		1,		0}),	"LEVCAN received",	0),
					 { pti(RD.Debug.ADCTimerOutput	,			0, 		2, 		1,		0),		PT_enum ,			"ADC Timer Output", 	"Off\nStart\nSample\nError" }, //
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Debug.FOCfixedVdq, 			"FOC fixed Vdq",	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		HWConst.Timer.SampleAmpTick,			((LCP_Uint32_t){			10, 	500, 	5,		0}),			"Sample Amp tick", 	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		HWConst.Timer.SampleAmpReal_ns ,			((LCP_Uint32_t){		0, 		0, 		5,		1}),	"Sample Amp real", 	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		HWConst.Timer.SampleBEMFTick,			((LCP_Uint32_t){			10, 	1000, 	5,		0}),			"Sample BEMF ns", 	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		HWConst.Timer.SampleBEMFReal_ns ,			((LCP_Uint32_t){		0, 		0, 		5,		1}),	"Sample BEMF real", 	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		HWConfig.Timings.DeadTime_ns,			((LCP_Uint32_t){			200, 	1500, 	5,		0}),			"Dead time ms", 	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		HWConst.Timer.DeadTimeReal_ns ,			((LCP_Uint32_t){		200, 	1500, 	5,		1}),	"Dead time real", 	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Debug.ADCstream0_max,			((LCP_Uint32_t){				0, 		0, 		0,		0}), "ADC stream 0 HZ", 	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Debug.ADCstream2_max,			((LCP_Uint32_t){				0, 		0, 		0,		0}), "ADC stream 2 HZ", 	0),

};
#endif

const                char logratetext[] = "PWM1\nPWM2\nPWM5\nPWM10\n1ms\n10ms\n20ms\n100ms\n1s\n5s\n10s\n30s";
LCPS_Entry_t         PD_Logger[] = {
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Logger.Start, 			LANG("Start logging", "Запустить запись"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Logger.Stop, 			LANG("Stop logging", "Остановить запись"), 0),
					 { pti(RD.Logger.State, 						0, 		1, 		1,		0),	PT_enum | PT_readonly,	LANG("# State", "# Состояние"), LANG("Off\nRecording\nError\nStopped\nWaiting", "Отключен\nЗапись\nОшибка\nОстановлен\nОжидание")}, //
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Logger.Error,			((LCP_Uint32_t){ 						0, 		1, 		1,		0}),	LANG("# Error code", "# Код ошибки"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Logger.StartMode, 			((LCP_Enum_t){0, Logger_StartMode_MAX - 1}),			LANG("Start mode", "Режим запуска"), LANG("Manual\nAt start", "Ручной\nПри включении")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Logger.Time, 			((LCP_Enum_t){0, 1}),			LANG("Log time", "Время в логе"), LANG("Sys time\nTime step", "Системное\nШаг времени")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Logger.Rate, 			((LCP_Enum_t){0, LogRate_MAX - 1}),			LANG("Log rate", "Период записи"), logratetext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Logger.Mode, 			((LCP_Enum_t){0, 1}),			LANG("Mode", "Режим записи"), LANG("Buffered\nMax rate", "Буффер\nМакс скорость")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Logger.Filter, 			((LCP_Enum_t){0, LogFilter_MAX - 1}),			LANG("Data averaging", "Усреднение данных"), LANG("None\nFast\nSlow","Нет\nБыстр\nМедл")),
					 { 0,							0, 			5500, 	10, 	0,		0,	PT_value | PT_readonly, LANG("# Data to log:", "# Данные для записи:"), " " }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				"Code fucked up sry :(", 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //
					 { 0, 							0, 			0, 		1, 		1,		0,	PT_bool,				0, 0 }, //

};

uint32_t             dummy;
LCPS_Entry_t         PD_About[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		dummy,			((LCP_Uint32_t){ 								0, 		1, 		1,		0}),	0,	" "),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		HWConfig.Limits.SupplyMaxV,			((LCP_Uint32_t){ 			0, 		1, 		1,		1}),	LANG("Max supply", "Предел напряжения"),	 "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		HWConfig.Limits.PhaseMaxA,			((LCP_Uint32_t){ 			0, 		1, 		1,		1}),	LANG("Max current", "Максимальный ток"), "%sА"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		dummy,			((LCP_Uint32_t){ 								0, 		1, 		1,		0}),	LANG("Firmware date", "Дата прошивки"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		dummy,			((LCP_Uint32_t){ 								0, 		1, 		1,		0}),	LANG("Firmware ver.", "Версия прошивки"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		dummy,			((LCP_Uint32_t){ 								0, 		1, 		1,		0}),	LANG("Loader date", "Дата загрузчика"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		dummy,			((LCP_Uint32_t){ 								0, 		1, 		1,		0}),	LANG("Loader version", "Версия загрузчика"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		LifeData.TotalkWH,			((LCP_Uint32_t){ 					0, 		1, 		1,		0}),	LANG("Worked", "Наработка"), "%skW*h"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		LifeData.MiddleTemp100hr,			((LCP_Uint32_t){ 			0, 		1, 		1,		1}),	LANG("t° middle 100h", "t° сред. за 100ч"), "%s°C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		LifeData.MiddleTemp,			((LCP_Uint32_t){ 					0, 		1, 		1,		1}),	LANG("t° middle", "t° сред."), "%s°C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		LifeData.OVCRCounter,			((LCP_Uint32_t){ 				0, 		1, 		1,		0}),	LANG("Current protections", "Защит по току"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		LifeData.OVTCounter,			((LCP_Uint32_t){ 					0, 		1, 		1,		0}),	LANG("Temperature protections", "Защит по температуре"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		LifeData.OVVCounter,			((LCP_Uint32_t){ 					0, 		1, 		1,		0}),	LANG("Voltage protections", "Защит по напряжению"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		LifeData.PowerCycleCount,			((LCP_Uint32_t){ 			0, 		1, 		1,		0}),	LANG("Power cycle", "Включений"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Lifetime.Min,			((LCP_Uint32_t){ 						0, 		1, 		1,		0}),	LANG("Power-on time", "Время работы"), LANG("%s min", "%s мин.")),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Lifetime.Hours,			((LCP_Uint32_t){ 					0, 		1, 		1,		0}),	"-", 	LANG("%s h", "%s ч.")),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Lifetime.Days,			((LCP_Uint32_t){ 					0, 		1, 		1,		0}),	"--", 	LANG("%s days", "%s дн.")),
};

const                LCPS_Entry_t PD_Misc[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Misc.EnableMode, 			((LCP_Enum_t){0, 3}),	LANG("Disable button", "Кнопка включения"), LANG("None\nSwitch\nButton\nCAN", "Отключена\nПерекл.\nКнопка\nCAN")),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.Misc.AutoShutdown , 	LANG("Auto shutdown", "Автоотключение"),0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Misc.ShutdownTime,			((LCP_Uint32_t){ 			30, 	1500, 	5,		0}),	LANG("Sleep time", "Время сна"), "%s s"),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.Misc.LockAtEnable, 	LANG("Lock at turn-on", "Блокировка при вкл."),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.Misc.SpeedCalc, 	LANG("Speed calculation", "Расчет скорости"),	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Misc.SpeedCircLen_mm,			((LCP_Uint32_t){ 			0, 		3000, 	5,		0}),	LANG("Circumference", "Длина окружности"), LANG("%s mm", "%s mm")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Misc.MotorSprocket,			((LCP_Uint32_t){ 			1, 		5000, 	1,		0}),	LANG("Motor sprocket", "Звезда мотора"), LANG("%s t", "%s зуб.")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Misc.WheelSprocket,			((LCP_Uint32_t){ 			1, 		5000, 	1,		0}),	LANG("Wheel sprocket", "Звезда колеса"), LANG("%s t", "%s зуб.")),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.Misc.Master, 	LANG("Master-controller", "Мастер-контроллер"),	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Misc.CPUtLim,			((LCP_Uint32_t){ 					60, 	105, 	5,		0}),	LANG("Limit t° CPU", "Лимит t° CPU"), "%s°C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Data.NodeID,			((LCP_Uint32_t){ 					0, 	LC_Null_Address - 1, 	1,		0}),	LANG("Device ID", "ID устройства"),	0),
};

const                LCPS_Entry_t PD_PortState[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.Enable,			((LCP_Uint32_t){ 					0, 		0, 		1, 		0}),	LANG("Enable button", "Кнопка вкл."), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.S1,			((LCP_Uint32_t){ 						0, 		0, 		1, 		0}),	"S1", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.S3,			((LCP_Uint32_t){ 						0, 		0, 		1, 		0}),	"S3", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.RV,			((LCP_Uint32_t){ 						0, 		0, 		1, 		0}),	"RV", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.CR,			((LCP_Uint32_t){ 						0, 		0, 		1, 		0}),	"CR", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.P1,			((LCP_Uint32_t){ 						0, 		0, 		1, 		0}),	"P1", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.P2,			((LCP_Uint32_t){ 						0, 		0, 		1, 		0}),	"P2", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.P,			((LCP_Uint32_t){ 							0, 		0, 		1, 		0}),	"P", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.M,			((LCP_Uint32_t){ 							0, 		0, 		1, 		0}),	"M", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.Embedd.VThrottle,			((LCP_Uint32_t){ 			0, 		0, 		0, 		3}), LANG("# Throttle","# Ручка газа"),"%s V"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.Embedd.VBrake,			((LCP_Uint32_t){				0, 		0, 		0, 		3}), LANG("# Brake", "# Ручка тормоза"),"%s V"),
};
/*
					 PF_CruiseInc,
					 PF_CruiseDec,
					 PF_CruiseRestore,*/
const                char buttext[] = "OFF\nRV\nCRe\nCR+\nCR-\nCRr\nBK\nDM\nDTH\nDPAS\nSWSNS\nN\nnBK\nS1\nS2\nS3\nS1of3\nS3of3\nScyc\nS++\nS--\nSPSNS\nSpec.";
const                LCPS_Entry_t PD_PortConfig[] = {
					 folder(LCP_AccessLvl_Any, 	Dir_PortState, 					0, 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.SpeedMode, 			((LCP_Enum_t){0, 1}),	LANG("Speeds mode", "Режим скоростей"), LANG("Switch\nButtons", "Перекл.\nКнопки")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.S1, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("Port S1", "Порт S1"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.S3, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("Port S3", "Порт S3"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.RV, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("Port RV", "Порт RV"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.CR, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("Port CR", "Порт CR"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.P1, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("Port P1", "Порт P1"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.P2, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("Port P2", "Порт P2"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.P, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("Port P", "Порт P"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.M, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("Port M", "Порт M"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT1, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 1", "CAN порт 1"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT2, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 2", "CAN порт 2"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT3, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 3", "CAN порт 3"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT4, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 4", "CAN порт 4"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT5, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 5", "CAN порт 5"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT6, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 6", "CAN порт 6"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT7, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 7", "CAN порт 7"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT8, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 8", "CAN порт 8"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT9, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 9", "CAN порт 9"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT10, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 10", "CAN порт 10"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT11, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 11", "CAN порт 11"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT12, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 12", "CAN порт 12"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT13, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 13", "CAN порт 13"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT14, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 14", "CAN порт 14"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT15, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 15", "CAN порт 15"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT16, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 16", "CAN порт 16"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM_Freq, 			((LCP_Enum_t){0, 5}),	LANG("PWM freq", "Выход ШИМ"), "100Hz\n500Hz\n1kHz\n5kHz\n10kHz\n25kHz(FAN)"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM1, 			((LCP_Enum_t){0, 4}),	LANG("PWM P1", "ШИМ P1"), LANG("Disabled\nStop-light\nHeadligth\nt°C motor\nt°C controller\nPort busy!", "Отключен\nСтоп-огни\nГабариты\nt°C мотора\nt°C контроллера\nПорт занят!")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM1Min,			((LCP_Uint32_t){ 			0, 		100, 		1,		0}),	LANG("PWM P1 Min", "ШИМ P1 Минимум"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM1Max,			((LCP_Uint32_t){  			0, 		100, 		1,		0}),	LANG("PWM P1 Max", "ШИМ P1 Максимум"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM1TempMin,			((LCP_Uint32_t){ 		0, 		100, 		1,		0}),	LANG("PWM P1 t° Min", "ШИМ P1 t° Минимум"), "%s°C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM1TempMax,			((LCP_Uint32_t){  		0, 		100, 		1,		0}),	LANG("PWM P1 t° Max", "ШИМ P1 t° Максимум"), "%s°C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM2, 			((LCP_Enum_t){0, 4}),	LANG("PWM P2", "ШИМ P2"), LANG("Disabled\nStop-light\nHeadligth\nt°C motor\nt°C controller\nPort busy!", "Отключен\nСтоп-огни\nГабариты\nt°C мотора\nt°C контроллера\nПорт занят!")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM2Min,			((LCP_Uint32_t){ 			0, 		100, 		1,		0}),	LANG("PWM P2 Min", "ШИМ P2 Минимум"), "%s°C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM2Max,			((LCP_Uint32_t){  			0, 		100, 		1,		0}),	LANG("PWM P2 Max", "ШИМ P2 Максимум"), "%s°C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM2TempMin,			((LCP_Uint32_t){ 		0, 		100, 		1,		0}),	LANG("PWM P2 t° Min", "ШИМ P2 t° Минимум"), "%s°C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM2TempMax,			((LCP_Uint32_t){  		0, 		100, 		1,		0}),	LANG("PWM P2 t° Max", "ШИМ P2 t° Максимум"), "%s°C"),
};

extern               const LCPS_Directory_t pDirectories[] = { //
					 directory(PD_Root,				0, LCP_AccessLvl_Any, "Controller"),
					 directory(PD_Autoconf,			0, LCP_AccessLvl_Any, LANG("Auto-setup", "Автонастройка")),
					 directory(PD_Controls,			0, LCP_AccessLvl_Any, LANG("Control", "Управление")),
					 directory(PD_ControlModes,		0, LCP_AccessLvl_Any, LANG("Control modes", "Режимы управления")),
					 directory(PD_AdvancedModes,		0, LCP_AccessLvl_Any, LANG("Advanced modes", "Дополнительные режимы")),
					 directory(PD_Advanced,			0, LCP_AccessLvl_Any, LANG("Mode S1", "Режим S1")),
					 directory(PD_Advanced,			1, LCP_AccessLvl_Any, LANG("Mode S2", "Режим S2")),
					 directory(PD_Advanced,			2, LCP_AccessLvl_Any, LANG("Mode S3", "Режим S3")),
					 directory(PD_PID,				0, LCP_AccessLvl_Any, LANG("PID regulators", "PID регуляторы")),
					 directory(PD_Motor,				0, LCP_AccessLvl_Any, LANG("Motor setup", "Настройка мотора")),
					 directory(PD_MotorTsensor,		0, LCP_AccessLvl_Any, LANG("Motor t°-sensor", "Термодатчик мотора")),
					 directory(PD_MotorHallTable,	0, LCP_AccessLvl_Any, LANG("Hall table", "Таблица холлов")),
					 directory(PD_Battery,			0, LCP_AccessLvl_Any, LANG("Battery", "Батарея")),
					 directory(PD_DCDC,				0, LCP_AccessLvl_Any, LANG("Converter", "Преобразователь")),
					 directory(PD_flags,				0, LCP_AccessLvl_Any, LANG("Status flags", "Флаги статуса")),
					 directory(PD_Clutch,			0, LCP_AccessLvl_Any, LANG("Clutch", "Муфта")),
					 directory(PD_About,				0, LCP_AccessLvl_Any, LANG("Device information", "Информация об устройстве")),
					 directory(PD_Tune,				0, LCP_AccessLvl_Any, LANG("Manual angle setup", "Ручная настройка угла")),
					 directory(PD_DebugInfo,			0, LCP_AccessLvl_Any, LANG("Debug information", "Отладочная информация")),
					 directory(PD_Misc,				0, LCP_AccessLvl_Any, LANG("Extra parameters", "Доп. настройки")),
					 directory(PD_PortState,			0, LCP_AccessLvl_Any, LANG("Port state", "Состояние портов")),
					 directory(PD_PortConfig,		0, LCP_AccessLvl_Any, LANG("I/O configuration", "Настройка портов")),
					 directory(PD_PAS,				0, LCP_AccessLvl_Any, "Pedal Assist System"),
					 directory(PD_Debug,				0, LCP_AccessLvl_Dev, "Debug"),
					 directory(PD_Menu,				0, LCP_AccessLvl_Any, LANG("Updates and settings", "Настройки и обновление")),
					 directory(PD_DebugFoc,			0, LCP_AccessLvl_Any, LANG("Debug FOC", "Отладка FOC")),
					 directory(PD_DebugRemote,		0, LCP_AccessLvl_Any, LANG("Remote inputs", "Удаленное управление")),
					 directory(PD_Logger,			0, LCP_AccessLvl_Any, LANG("Logger", "Логгер")),
					 directory(PD_RCPWM,				0, LCP_AccessLvl_Any, LANG("RC Control", "RC Управление")),
					 directory(PD_Cruise,			0, LCP_AccessLvl_Any, LANG("Cruise", "Круиз")),
					 directory(PD_Curves,			0, LCP_AccessLvl_Any, LANG("Throttle/brake curves", "Кривые ручек газа/тормоза"))
};
//                   @formatter:on
extern               const int dirsize = ARRAYSIZ(pDirectories);
