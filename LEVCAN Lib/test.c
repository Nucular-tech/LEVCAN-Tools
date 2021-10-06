/*
					 * newparameters.c
					 *
					 *  Created on: 27 ���. 2020 �.
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
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal, RD.Menu.Save, LANG("Save settings", "��������� ���������"), 0),
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
					 label(LCP_AccessLvl_Any, 	0,			LANG("# Configure step-by-step! #", "# ��������� ��� �� �������! #"),	" "),
					 pstd(LCP_AccessLvl_Dev, 	LCP_Normal,  	RD.Configurator.BenchTest, 		((LCP_Enum_t){0, 1}), 	"Bench test", "Off\nEn\nRunning...\nOk\nFail"),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.All,									LANG("Full setup", "������ ���������"),	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.Brake, 			((LCP_Enum_t){0, 1}),	LANG("Brake", "����� �������"),		LANG("Off\nEn\nPress\nRelease\n��\nError" ,"����\n���\n�������\n���������\n��\n������")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.Throttle, 		((LCP_Enum_t){0, 1}),	LANG("Throttle", "����� ����"),		LANG("Off\nEn\nPress\nRelease\n��\nError","����\n���\n�������\n���������\n��\n������")),
					 //	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.MotorLR, 		((LCP_Enum_t){0, 1}),	LANG("Motor LR", "����� LR"),		LANG("Off\nEn\nOK\nStopped","����\n���\n��\n����������") ),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.Motor, 			((LCP_Enum_t){0, 1}),	LANG("Motor", "�����"),				LANG("Off\nEn\nOK\nAcceleration\nIndexing\nStopped\nHall error\nProtection\nTimeout","����\n���\n��\n������\n����������\n����������\n������ ��������\n������\n�������")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.HallAjust, 		((LCP_Enum_t){0, 1}),	LANG("Angle correction", "������������� ����"),LANG("Off\nEn\nOK\nAcceleration\nAngle corr.\nStopped\nProtection\nTimeout","����\n���\n��\n������\n���������..\n����������\n������\n�������")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.DetectCurrent,	((LCP_Uint32_t){2, 50, 1, 0}),	LANG("Setup current", "��� ���������"), 	"%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.PolePairs,			((LCP_Uint32_t){1, 50, 1, 0}),	LANG("Pole pair", "��� �������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.InvertDirection, 	((LCP_Enum_t){0, 1}),	LANG("Spin direction", "����������� ��������"), LANG("Forward\nInvert", "������\n��������")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Source, 			((LCP_Enum_t){0, 3}),	LANG("Control source", "�������� ������."),LANG("Disabled\nAuto\nEmbedd\nRemote","��������\n����\n����������\n���������")),
					 //	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.SensorMode, 				0, 		4, 		1, 		0),			PT_enum,LANG(,"����� ����������", "���������\n��������\n�����������\nFOC\nHz"  ),
					 //	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RuntimeData.Configurator.Motor, 0, 	1, 		1,		0, 	PT_uint8,		RT_bool,LANG(,"��������� ������",	0 ),
};

const                char preset_names[] = LANG("None\nLinear\nExponential\nNormal\nPolynomial", "���\n��������\n����������\n����������\n�������");
const                LCPS_Entry_t PD_Curves[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Menu.CurveThrottlePreset, 			((LCP_Enum_t){0, CurvePreset_MAX - 1}),	LANG("Throttle preset", "������ ����"), preset_names),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.ThrottleCurve[0],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Throttle start", "��� ������"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.ThrottleCurve[1],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Throttle 1", "��� 1"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.ThrottleCurve[2],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Throttle 2", "��� 2"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.ThrottleCurve[3],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Throttle 3", "��� 3"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.ThrottleCurve[4],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Throttle 4", "��� 4"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.ThrottleCurve[5],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Throttle 5", "��� 5"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.ThrottleCurve[6],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Throttle 6", "��� 6"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.ThrottleCurve[7],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Throttle end", "��� �����"), "%s%%"),

					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Menu.CurveBrakePreset, 			((LCP_Enum_t){0, CurvePreset_MAX - 1}),	LANG("Brake preset", "������ �������"), preset_names),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.BrakeCurve[0],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Brake start", "������ ������"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.BrakeCurve[1],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Brake 1", "������ 1"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.BrakeCurve[2],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Brake 2", "������ 2"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.BrakeCurve[3],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Brake 3", "������ 3"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.BrakeCurve[4],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Brake 4", "������ 4"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.BrakeCurve[5],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Brake 5", "������ 5"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.BrakeCurve[6],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Brake 6", "������ 6"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.BrakeCurve[7],			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("Brake end", "������ �����"), "%s%%"),
};


const                LCPS_Entry_t PD_Cruise[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Cruise.Mode, 			((LCP_Enum_t){0, 4}),	LANG("Cruise", "�����"), LANG("Disabled\nButton\nSwitch\nThrottle hold\nAllow Throttle hold\nMotor assist", "��������\n������\n�������������\n��������� ����\n��������. ��������� ����\n��������� ������")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Cruise.Restore, 			((LCP_Enum_t){0, 1}),	LANG("Cruise restore", "���������. ������"), LANG("Disabled\nButton", "��������\n������")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Cruise.Delay,			((LCP_Uint32_t){ 			25, 	3000, 	25,		2}),	LANG("Cruise EN time", "����� ���. ������"), "%s s"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Cruise.ThrottleDelta,			((LCP_Uint32_t){ 	1, 		30, 	1,		0}),	LANG("Cruise by throttle", "����� �� ����� ����"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Cruise.Level, 			((LCP_Enum_t){0, 2}),	LANG("Cruise level", "������� ������"), LANG("Mixed\nThrottle\nSpeed", "���������\n�� ����\n�� ��������")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Cruise.dERPM_ds,			((LCP_Uint32_t){ 		0, 		50000, 50,		0}),	LANG("Cruise smoothness", "��������� ������"), "%sERPM/s"),
					 label(LCP_AccessLvl_Any, 	0,		LANG("# Used for cruise activation:", "# ������������ ��� ���. ������:"),	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Cruise.SafeAcc,			((LCP_Uint32_t){ 		100, 	10000, 	100,	0}),	LANG("Safe acceleration", "��������. ���������"), "%sERPM/s"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.Cruise.MinSpeed,			((LCP_Uint32_t){ 		3, 		127, 	1,		0}),	LANG("Min. speed", "���. ��������"), "%skm/h"),

};

const                LCPS_Entry_t PD_PAS[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.Mode, 			((LCP_Enum_t){0, 2}),	"PAS", LANG("Disabled\nPAS sensor\nTorque sensor\nPort is busy!", "��������\n������ PAS\n������ ��������\n���� �����!")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.Wires, 			((LCP_Enum_t){0, 1}),	LANG("PAS connection", "�����������"), LANG("1-wire\n2-wire", "1-������\n2-�������")),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.Control.PAS.InvertDir, 	LANG("Invert PAS", "������������� PAS"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.Poles,			((LCP_Uint32_t){ 			1, 		200, 	1,		0}),	LANG("PAS poles", "PAS �������"), 	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.MinFreq,			((LCP_Uint32_t){ 			1, 		500, 	1,		0}),	LANG("PAS min freq.", "PAS ���. �������"), "%s RPM"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.MaxFreq,			((LCP_Uint32_t){ 			10, 	1000, 	5,		0}),	LANG("PAS max freq.", "PAS ����. �������"), "%s RPM"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.PAS.PAS_freq_filtered,			((LCP_Uint32_t){ 	0, 		0, 		0,		0}),LANG("# PAS freq.", "# PAS �������"), "%s RPM"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.Timeout,			((LCP_Uint32_t){ 			2, 		500, 	2,		2}),	LANG("PAS timeout", "PAS �������"), LANG("%s sec", "%s ���")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.FilterHz,			((LCP_Uint32_t){ 			1, 		100, 	1,		0}),	LANG("PAS filter", "PAS ������"), "%s Hz"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.Torq_V,			((LCP_Uint32_t){ 			0, 		1000, 	5,		1}),	LANG("Pressure scale", "����� ��������"), "%s Nm/V"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.ZeroTorq,			((LCP_Uint32_t){ 			0, 		10000, 	10,		0}),	LANG("Zero pressure", "������� ��������"), "%s mV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.ProtectionTorq,			((LCP_Uint32_t){ 	0, 		10000, 	100,	0}),	LANG("Protection pressure", "������� ������"), "%s mV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.TorqAvg,			((LCP_Uint32_t){ 			1, 		20, 	1,		0}),	LANG("Torque averaging", "���������� ����"), LANG("%s turn/2", "%s ��/2")),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.PAS.PAStorque,			((LCP_Uint32_t){ 			0, 		0, 		0,		1}),LANG("# Torque", "# ������"), "%s Nm"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.PAS.PASwatts,			((LCP_Uint32_t){ 				0, 		0, 		0,		1}),LANG("# Human watt", "# ��������"), "%s W"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.MinWatt,			((LCP_Uint32_t){ 			0, 		500, 	10,		0}),	LANG("Human watt min", "�������� ���."), "%s W"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.MaxWatt,			((LCP_Uint32_t){ 			0, 		1000, 	10,		0}),	LANG("Human watt max", "�������� ����."), "%s W"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.MinTorq,			((LCP_Uint32_t){ 			0, 		100, 	2,		0}),	LANG("Torque min", "���� ���."), "%s Nm"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.MaxTorq,			((LCP_Uint32_t){ 			0, 		300, 	2,		0}),	LANG("Torque max", "���� ����."), "%s Nm"),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.Control.PAS.InstantTorq, 	LANG("Instant Torque", "���������� ����"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.MinOutput,			((LCP_Uint32_t){ 		0, 		100, 	1,		0}),	LANG("PAS min out", "PAS ���. �����"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.PAS.MaxOutput,			((LCP_Uint32_t){ 		2, 		100, 	1,		0}),	LANG("PAS max out", "PAS ����. �����"), "%s%%"),
};

const                LCPS_Entry_t PD_RCPWM[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.RCPWM.Mode, 			((LCP_Enum_t){0, 1}),	LANG("Input P1 mode", "����� ����� P1"),LANG("Off\nPWM\nError", "����.\nPWM\n������")),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.RCPWM.Freq,			((LCP_Uint32_t){ 				0, 		0, 		0,		1}),LANG("# Input Freq.", "# ������� �����"), "%sHZ"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.RCPWM.Period,			((LCP_Uint32_t){ 				0, 		0, 		0,		2}),LANG("# Width", "# �����"), "%sms"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.RCPWM.Function, 			((LCP_Enum_t){0, 3}),	LANG("Function", "�������"),LANG("Off\nThrottle\nBrake\nThrottle and brk.", "����.\n���\n������\n��� � ��������")),
					 label(LCP_AccessLvl_Any, 	0,		LANG("# Throttle range", "# �������� ����"),0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.RCPWM.Throttle.Min,			((LCP_Uint32_t){ 	0, 		1000, 	1,		2}),	LANG("Throttle min", "��� ���"),"%sms"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.RCPWM.Throttle.Max,			((LCP_Uint32_t){ 	0, 		1000, 	1,		2}),	LANG("Throttle max", "��� ����"),"%sms"),
					 label(LCP_AccessLvl_Any, 	0,		LANG("# Brake range", "# �������� �������"),0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.RCPWM.Brake.Min,			((LCP_Uint32_t){ 		0, 		1000, 	1,		2}),	LANG("Brake min", "������ ���"),"%sms"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Control.RCPWM.Brake.Max,			((LCP_Uint32_t){ 		0, 		1000, 	1,		2}),	LANG("Brake max", "������ ����"),"%sms"),
};

enum {
	PhaseMAX = 500,
	BattIMAX = 400,
	SpeedMAX = 150,
	PowMAX = 300
};
const                LCPS_Entry_t PD_ControlModes[] = {
					 { pti(RD.Control.Motor.Ports.Speed, 		0, 		0, 		0,		0),	PT_enum | PT_readonly, LANG("# Selected mode:", "# ������� �����:"), "N\nS1\nS2\nS3" }, //
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Torque[0],			((LCP_Uint32_t){ 				0, 	PhaseMAX, 	2,		0}),	LANG("Phase 1", "������ 1"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.BatteryI[0],			((LCP_Uint32_t){ 			2, 	BattIMAX, 	1,		0}),	LANG("Battery 1", "���������� 1"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Power[0],			((LCP_Uint32_t){ 				0, 	PowMAX, 	1,		1}),	LANG("Power 1", "�������� 1"), "%skW"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Speed[0],			((LCP_Uint32_t){ 				4, 	SpeedMAX, 	2,		0}),	LANG("Speed 1", "�������� 1"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Torque[1],			((LCP_Uint32_t){ 				0, 	PhaseMAX, 	2,		0}),	LANG("Phase 2", "������ 2"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.BatteryI[1],			((LCP_Uint32_t){ 			2, 	BattIMAX, 	1,		0}),	LANG("Battery 2", "���������� 2"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Power[1],			((LCP_Uint32_t){ 				0, 	PowMAX, 	1,		1}),	LANG("Power 2", "�������� 2"), "%skW"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Speed[1],			((LCP_Uint32_t){ 				4, 	SpeedMAX, 	2,		0}),	LANG("Speed 2", "�������� 2"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Torque[2],			((LCP_Uint32_t){ 				0, 	PhaseMAX, 	2,		0}),	LANG("Phase 3", "������ 3"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.BatteryI[2],			((LCP_Uint32_t){ 			2, 	BattIMAX, 	1,		0}),	LANG("Battery 3", "���������� 3"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Power[2],			((LCP_Uint32_t){ 				0, 	PowMAX, 	1,		1}),	LANG("Power 3", "�������� 3"), "%skW"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Speed[2],			((LCP_Uint32_t){ 				4, 	SpeedMAX, 	2,		0}),	LANG("Speed 3", "�������� 3"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.BrakeTorque,			((LCP_Uint32_t){ 			0, 		500, 	2,		0}),	LANG("Braking phase", "������ ����������"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.SpeedBrakeTorque,			((LCP_Uint32_t){ 		0, 		500, 	2,		0}),	LANG("Braking ph. at speed", "���.����. ��� ���.����."), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.RevSpeed,			((LCP_Uint32_t){ 				2, 		150, 	2,		0}),	LANG("Speed reverse", "�������� �������"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.RevTorque,			((LCP_Uint32_t){ 				10, 	500, 	2,		0}),	LANG("Phase reverse", "������ �������"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.Weakening,			((LCP_Uint32_t){ 				0,	 	500, 	2,		0}),	LANG("Field weakening", "����������"), "%sA"),
					 label(LCP_AccessLvl_Any, 	0,LANG("# Current change speed:", "# �������� ��������� ����:"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.dmA_dms_RisePos,			((LCP_Uint32_t){ 		50, 	50000, 	100,	0}),	LANG("- acceleration", "- �������"), "%sA/s"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.dmA_dms_RiseNeg,			((LCP_Uint32_t){ 		50, 	50000, 	100,	0}),	LANG("- braking", "- ����������"), "%sA/s"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Modes.dmA_dms_Fall,			((LCP_Uint32_t){ 			500, 	50000, 	100,	0}),	LANG("- shutdown", "- ����������"), "%sA/s"),
};
const                LCPS_Entry_t PD_AdvancedModes[] = {
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.AdvancedEnable, 	LANG("Enable adv. modes", "�������� ���. ������"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.DefaultSpeedNeutral, 	LANG("Neutral by default", "�������� �� �����."), 0),
					 folder(LCP_AccessLvl_Any, 	Dir_AdvancedMode1, 					0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_AdvancedMode2, 					0, 0),
					 folder(LCP_AccessLvl_Any, 	Dir_AdvancedMode3, 					0, 0),
};

const                LCPS_Entry_t PD_Advanced[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.AdvancedModes.ThrottleModes[0], 			((LCP_Enum_t){0, 2}),	LANG("Throttle mode", "���. ����� ����"), LANG("Speed+torque\nSpeed\nTorque" , "�������� � ����\n��������\n����")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.AdvancedModes.dERPM_ds_Accs[0],			((LCP_Uint32_t){ 	0, 		500000, 200,	0}),	LANG("Acceleration lim.", "����� ���������"), "%sERPM/s"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.AdvancedModes.dERPM_ds_Brakes[0],			((LCP_Uint32_t){ 	0, 		500000, 200,	0}),	LANG("Deceleration lim.", "����� ����������"), "%sERPM/s"),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.Reverses[0], 	LANG("Reverse", "������"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.Cruises[0], 	LANG("Cruise", "�����"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.DisableMotors[0], 	LANG("Disable motor", "��������� �����"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.DisableThrottles[0], 	LANG("Disable throttle", "��������� ����� ����"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.BrakeAtEnds[0], 	LANG("Active braking", "�������� ����������"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.ReverseAtBrake[0], 	LANG("Reverse on brake", "�������� ��� ��� �������"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.SpeedControlAtZeros[0], 	LANG("Speed lim. at 0% throttle", "����� ��. ��� 0% ����"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.AdvancedModes.DisablePASs[0], 	LANG("Disable PAS", "��������� PAS"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.AdvancedModes.PASScale[0],			((LCP_Uint32_t){ 		1, 		100, 	1,		0}),	LANG("PAS Scale", "PAS ����������"), "%s%%"),
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
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.PolePairs,			((LCP_Uint32_t){				1, 		50, 	1, 		0}),	LANG("Pole pair", "��� �������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.InvertDirection, 			((LCP_Enum_t){0, 1}),	LANG("Spin direction", "����������� ��������"), LANG("Forward\nInvert", "������\n��������")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.IntegrationThreshold,			((LCP_Uint32_t){	0, 	INT32_MAX, 100,		3}),	LANG("Integration thr.", "����� ��������������"), "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.SensorModePending, 			((LCP_Enum_t){0, 5}),	LANG("Control mode now", "���.���.�����."), LANG("Sensorless\nSquare\nCombined\nFOC\nHz\nSine HZ", "���������\n��������\n�����������\nFOC\nHz\nSine HZ")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.SensorMode, 			((LCP_Enum_t){0, 5}),	LANG("Control mode", "����� ����������"), LANG("Sensorless\nSquare\nCombined\nFOC\nHz\nSine HZ", "���������\n��������\n�����������\nFOC\nHz\nSine HZ")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.CombinedTransition,			((LCP_Uint32_t){ 		0, 		200, 	5,		2}),	LANG("From hall to s-less", "�����->���������"), "%s rad/s"),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.PositionSensor.Interpolation, 	LANG("Interpolate halls", "������������ ������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.InterpolationStart,			((LCP_Uint32_t){ 0, 	100, 	1,		0}),	LANG("Interpolation start", "������ ������������"), "%s rad/s"),
					 //	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.MaxFreq,			((LCP_Uint32_t){ 				15000, 	25000, 	1000,	0}),LANG(,"����. �������"), "%sHZ"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.VFD,			((LCP_Uint32_t){ 								10, 	200, 	1,		0}),	LANG("Frequency control", "��������� ����������"), "%sHZ"),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.Motor.SquareBoost, 	LANG("Boost square current", "��������� ���� ��������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.BoostCurrent,			((LCP_Uint32_t){ 			0, 		100, 	1,		0}),	LANG("Boost current", "��� ��������"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.BoostSpeed,			((LCP_Uint32_t){ 				0, 		200, 	5,		2}),	LANG("Boost speed", "�������� ��������"), "%s rad/s"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.MaxV,			((LCP_Uint32_t){ 					0, 		100, 	1,		0}),	LANG("Max motor U", "���� ���������� ������"), "%sV"),
					 { pti(Config.Motor.kV, 						0, 		INT32_MAX, 	0,	1),			PT_value ,	"kV", "%seRPM/V"}, //
					 //	{ pti(Config.Motor.Rph, 					0, 		INT32_MAX, 	0,	4),			PT_value ,	"R phase", "%s Ohm"}, //
					 //	{ pti(Config.Motor.Ld, 						0, 		INT32_MAX, 	0,	1),			PT_value ,	"Ld", "%s uH"}, //
					 //	{ pti(Config.Motor.Lq, 						0, 		INT32_MAX, 	0,	1),			PT_value ,	"Lq", "%s uH"}, //
};

const                LCPS_Entry_t PD_Tune[] = {
	//	pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.Iabs_filter,			((LCP_Uint32_t){ 				0, 		0, 		0, 		3}),LANG("# Phase amps", "# ����. ���"), "%sA"),
	//	pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.DCBus.Ibus_filter,			((LCP_Uint32_t){ 				0, 		0, 		0, 		3}),LANG("# Battery amps", "# ���. ���"), "%sA"),
	//	pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.DCBus.Watts,			((LCP_Uint32_t){ 						0, 		0, 		0, 		0}),LANG("# Power", "# ��������"), "%sW"),
	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.SquareOffset,			((LCP_Uint32_t){ 	-30, 	30, 	1, 		0}),	LANG("Offset for square", "����� ��� ��������"), "%s�"),
	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.HallManualOffsetFW,			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset total fwd", "����� ����� ������"), "%s�"),
	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.HallManualOffsetRV,			((LCP_Uint32_t){	-60, 	60, 	1, 		0}),	LANG("Offset total bkwd", "����� ����� ��������"), "%s�"),
	pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Configurator.ResetOffsets, 	LANG("Reset angles", "����� �����"), 0),
	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.IntegrationThreshold,			((LCP_Uint32_t){	0, 	INT32_MAX, 100,		3}),	LANG("Integration threshold", "����� ��������������"), "%sV"),
	//	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.SensorModePending, 			((LCP_Enum_t){0, 4}),	LANG(,"���.���.�����."), "���������\n��������\n�����������\nFOC\nHz"  ),
	//	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.Motor, 			((LCP_Enum_t){0, 1}),	LANG(,"�����"),LANG(,"����\n���\n������..\n���� ����������\n��\n������\n������ ��������" ),
	//	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.HallAjust, 			((LCP_Enum_t){0, 1}),	LANG(,"������������� ����"),LANG(,"����\n���\n���� ����������\n���� ����\n������..\n��\n������"  ),
	pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Configurator.HallAjusterKi,			((LCP_Uint32_t){ 		2, 		500, 	2,		2}),	LANG("Hall adjust Ki", "����. �������������"), 0),
};

const                LCPS_Entry_t PD_MotorTsensor[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.TMax,			((LCP_Uint32_t){ 					500, 	2000, 	10, 	1}),	LANG("�t max", "�t ����."), "%s �C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.THysteresis,			((LCP_Uint32_t){	 			10, 	1000, 	10,  	1}),	LANG("Delta �t", "������ �t"), "%s �C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Motor.TSensType, 			((LCP_Enum_t){0, PT1000}),	LANG("Sensor type", "��� �������"), "OFF\nRAW\nTMP35\nTMP36\nTMP37\nKTY81(82)\nKTY83\nKTY84\nNTC10k 3950\nNTC10k 3380\nPT1000"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.Temp,			((LCP_Uint32_t){ 						0, 		6, 		1, 		1}), LANG("# Value �t #", "## �������� �t ##"), "%s �C"),
};

const                LCPS_Entry_t PD_MotorHallTable[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.PosSensor.HallInput,			((LCP_Uint32_t){ 				0, 		6, 		1, 		0}), LANG("# Hall input", "# Hall ����"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.StateCommutation,			((LCP_Uint32_t){ 					0, 		6, 		1, 		0}), LANG("# Step", "# ����������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.SquareOffset,			((LCP_Uint32_t){ 	-30, 	30, 	1, 		0}),	LANG("Square offset", "����� ��� ��������"), "%s�"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallSeq[0],			((LCP_Uint32_t){ 	0, 		6, 		1, 		0}),	"Hall 0", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallSeq[1],			((LCP_Uint32_t){ 	0,		6, 		1, 		0}),	"Hall 1", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallSeq[2],			((LCP_Uint32_t){ 	0,		6,		1, 		0}),	"Hall 2", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallSeq[3],			((LCP_Uint32_t){  	0,		6, 		1, 		0}),	"Hall 3", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallSeq[4],			((LCP_Uint32_t){ 	0,		6, 		1, 		0}),	"Hall 4", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallSeq[5],			((LCP_Uint32_t){ 	0,		6, 		1, 		0}),	"Hall 5", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallSeq[6],			((LCP_Uint32_t){ 	0,		6, 		1, 		0}),	"Hall 6", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallSeq[7],			((LCP_Uint32_t){  	0,		6, 		1, 		0}),	"Hall 7", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[0],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset fwd 1", "����� ������ 1"), "%s�"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[1],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset fwd 2", "����� ������ 2"), "%s�"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[2],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset fwd 3", "����� ������ 3"), "%s�"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[3],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset fwd 4", "����� ������ 4"), "%s�"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[4],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset fwd 5", "����� ������ 5"), "%s�"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[5],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset fwd 6", "����� ������ 6"), "%s�"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[6],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset bkwd 1", "����� �������� 1"), "%s�"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[7],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset bkwd 2", "����� �������� 2"), "%s�"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[8],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset bkwd 3", "����� �������� 3"), "%s�"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[9],			((LCP_Uint32_t){ 	-60, 	60, 	1, 		0}),	LANG("Offset bkwd 4", "����� �������� 4"), "%s�"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[10],			((LCP_Uint32_t){ -60, 	60, 	1, 		0}),	LANG("Offset bkwd 5", "����� �������� 5"), "%s�"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.PositionSensor.HallOffset[11],			((LCP_Uint32_t){ -60, 	60, 	1, 		0}),	LANG("Offset bkwd 6", "����� �������� 6"), "%s�"),
};

const                LCPS_Entry_t PD_Clutch[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.Mode, 			((LCP_Enum_t){0, 2}),	LANG("Mode", "�����"),LANG("OFF\nAccelerate\nAccelerate and hold", "����.\n������\n������ � ���������")),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.Iabs_filter,			((LCP_Uint32_t){ 				0, 		0, 		0, 		3}),LANG("# Phase amps", "# ����. ���"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.StartTime,			((LCP_Uint32_t){ 				1, 		20, 	1,		0}),	LANG("Start time", "����� �����"),"%ss"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.StartCurrent,			((LCP_Uint32_t){ 			2, 		500, 	2,		1}),	LANG("Start current", "��� �����"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.DetectTime_mS,			((LCP_Uint32_t){ 			10, 	1000, 	10,		0}),	LANG("Detection time", "����� ����"),"%sms"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.VoltageRise,			((LCP_Uint32_t){ 			2, 		1000, 	2,		0}),	LANG("Acceleration", "�������� �������"), "%sV/s"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.Hold.CurrentLow,			((LCP_Uint32_t){ 		2, 		500, 	2,		1}),	LANG("Hold (20%)", "��� ��������� (20%)"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.Hold.CurrentHigh,			((LCP_Uint32_t){ 		2, 		500, 	2,		1}),	LANG("Hold (80%)", "��� ��������� (80%)"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.Hold.StartTime,			((LCP_Uint32_t){ 		1, 		120, 	4,		0}),	LANG("Hold enable time", "����� ���. ���������"), "%ss"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Clutch.Hold.Time,			((LCP_Uint32_t){ 				1, 		120, 	1,		0}),	LANG("Hold time", "������������ ���������"), "%ss"),
};

const                LCPS_Entry_t PD_Battery[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Battery.ChargedV,			((LCP_Uint32_t){	 			0, 		1000,	10, 	2}),	LANG("Full charge", "������ �����"), "%sdV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Battery.MaxV,			((LCP_Uint32_t){ 					2000,	9500, 	10, 	2}),	LANG("Supply max", "������� ����"), "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Battery.MinV,			((LCP_Uint32_t){					1500, 	8000, 	10, 	2}),	LANG("Supply min", "������� ���"), "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Battery.MaxChrgA,			((LCP_Uint32_t){ 				10,		4000, 	5, 		1}),	LANG("Charge max", "����� ����"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Battery.MaxDscgA,			((LCP_Uint32_t){				10, 	4000, 	5, 		1}),	LANG("Discharge max", "������ ����"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Battery.MaxPower,			((LCP_Uint32_t){				0,	 	30000, 	100, 	0}),	LANG("Power max", "�������� ����"), "%sW"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.DCBus.Vbus_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0,		1}),	LANG("# DC voltage", "# DC ����������"), "%sV"),
};

const                LCPS_Entry_t PD_DCDC[] = {
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.DC_DC.Enable, 	LANG("Enable", "��������"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.DC_DC.Detect, 	LANG("Auto-Enable", "����-���������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.DC_DC.DetectPsuV,			((LCP_Uint32_t){ 				10, 	80, 	1,  	0}),	LANG("Detection threshold", "������� �������"), "%sVph"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.DC_DC.MaxBattA,			((LCP_Uint32_t){	 			0,		1000, 	5,  	1}),	LANG("Battery max I", "������� ���� ���"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.DC_DC.MinBattA,			((LCP_Uint32_t){ 				5, 		100, 	5,  	1}),	LANG("Battery min I", "������� ��� ���"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.DC_DC.UnderchargeV,			((LCP_Uint32_t){ 			0, 		1000, 	10,  	2}),	LANG("Undercharge", "���������"), "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.DC_DC.RampDelta,			((LCP_Uint32_t){ 				0, 		200, 	5,  	1}),	LANG("Current drop delta", "������ �������� ����"), "%sdV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.DC_DC.MaxPsuA,			((LCP_Uint32_t){ 				20, 	1500, 	5,  	1}),	LANG("Supply max I", "�� ���� ���"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.DC_DC.DropV,			((LCP_Uint32_t){ 					50, 	1000, 	25, 	2}),	LANG("Supply drop U", "������� U ��"), "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.DC_DC.MaxMotorT,			((LCP_Uint32_t){ 				50, 	120, 	5,  	0}),	LANG("Max motor t�", "���� t� ������"), "%s��"),
					 //	{ pti(Config.DC_DC.Phases, 					0, 		2, 		1, 		0),			RT_enum,	LANG(,"������. ���"), "����/n����/n���"}, //
					 //	{ pti(Config.DC_DC.Inductor, 				0, 		2, 		1, 		0),			RT_enum,	LANG(,"�������������"), "����\n�����\n��������"  }, //
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.DCBus.Ibus_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0, 		1}), LANG("# Battery I", "# ��� �������"), "%s�"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.DCBus.Vbus_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0, 		1}), LANG("# Battery U", "# ���������� �������"), "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.Iabs_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0, 		1}), LANG("# Supply I", "# ��� �������"), "%s�"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.Vabs_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0, 		1}), LANG("# Supply U", "# ���������� �������"), "%sV"),
};

const                LCPS_Entry_t PD_flags[] = {
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.ResetFlags, 	LANG("Reset?", "��������?"),	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Flags.Acceleration,			((LCP_Uint32_t){ 				0, 		0, 		0,		0}), LANG("Max acceleration", "����. ���������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Flags.Deceleration,			((LCP_Uint32_t){ 				0, 		0, 		0,		0}), LANG("Max deceleration", "����. ����������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Flags.LastCurrent,			((LCP_Uint32_t){ 				0, 		1, 		1,		0}),	LANG("Overload current", "��� ����������"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.Overcurrent, 	LANG("Overload", "���������� ����"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.Overweakening, 	LANG("Over-Field weakening", "���������� ����������"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.VBusOV, 	LANG("Supply overvoltage", "���������� U �������"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.VBusUV, 	LANG("Supply undervoltage", "������ U �������"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.V12Err, 	LANG("12V protection", "������ 12V"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.BrakeErr, 	LANG("Brake error", "������ �������"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.ThrottleErr, 	LANG("Throtle error", "������ ����"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.HallsErr, 	LANG("Hall error", "������ ������"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.CommutationErr, 	LANG("Code error", "������ ����"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.PASErr, 	LANG("PAS protection", "������ �� PAS"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.TempFETErr, 	LANG("Controller overheat", "�������� �����������"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.TempMotorErr, 	LANG("Motor overheat", "�������� ������"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.ProtectionFail, 	LANG("Protection fail", "������������� ������"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.PhaseVoltage, 	LANG("Voltage on phases", "���������� �� �����"),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Flags.CANErr, 	LANG("CAN: error", "CAN: ������"),	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Flags.CAN_LEC, 			((LCP_Enum_t){0, 7}),	"LEC",	"Ok\nStuff\nForm\nAcknowledgment\nBit recessive\nBit dominant\nCRC\nSW"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Flags.CAN_REC,			((LCP_Uint32_t){ 					0, 		255, 	1,		0}),	LANG("Receive w/error", "�������� � �������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Flags.CAN_TEC,			((LCP_Uint32_t){ 					0, 		255, 	1,		0}),	LANG("Sent w/error", "���������� � �������"),	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		RD.Flags.CAN_OVR, 			((LCP_Enum_t){0, 3}),	LANG("CAN state ", "CAN ����."), "Ok\nOVR0\nOVR1\nOVR01"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Flags.CAN_RX,			((LCP_Uint32_t){ 						0, 		0, 		0,		0}),	"CAN  RX",	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Flags.CAN_TX,			((LCP_Uint32_t){ 						0, 		0, 		0,		0}),	"CAN TX",	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Flags.USB_RX,			((LCP_Uint32_t){ 						0, 		0, 		0,		0}),	"USB RX",	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Flags.USB_TX,			((LCP_Uint32_t){ 						0, 		0, 		0,		0}),	"USB TX",	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.CPU_Usage,			((LCP_Uint32_t){ 						0,		0,		1, 		0}),	"CPU Load", "%s%%"),
};

const                LCPS_Entry_t PD_DebugInfo[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.System.TempFET,			((LCP_Uint32_t){					0, 		0, 		0,		1}),	LANG("Temp FET", "����������� FET"), "%s �C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.Temp,			((LCP_Uint32_t){						0, 		0, 		0,		1}),	LANG("Temp Motor", "����������� ������"), "%s �C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.DCBus.Ibus_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0,		1}),	LANG("DC current", "DC ���"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.DCBus.Vbus_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0,		1}),	LANG("DC voltage", "DC ����������"), "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.Iabs_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0,		1}),	LANG("AC current", "AC ���"), "%sA"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.Vabs_filter10Hz,			((LCP_Uint32_t){ 			0, 		0, 		0,		1}),	LANG("AC voltage", "AC ����������"), "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.V0_filter,			((LCP_Uint32_t){ 					0, 		0, 		0,		1}),	"Motor U0", "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.System.V12bus,			((LCP_Uint32_t){						0, 		0, 		0,		3}),	"System 12V", "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.System.V5bus,			((LCP_Uint32_t){						0, 		0, 		0,		3}),	"System 5V", "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.SpeedRPM,			((LCP_Uint32_t){					0, 		0, 		0,		0}),	"RPM", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Motor.SpeedERPM,			((LCP_Uint32_t){					0, 		0, 		0,		0}),	"ERPM", 0),
					 { pti(RD.PosSensor.HallInput, 				0, 		0, 		0,		0),	PT_enum | PT_readonly,	LANG("Hall input", "Hall ����"), "000\n001\n010\n011\n100\n101\n110\n111" },//
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.PosSensor.HallIndex,			((LCP_Uint32_t){ 				0, 		0, 		0,		0}),	LANG("Hall index", "Hall ������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.Motor.ThrottleFactor,			((LCP_Uint32_t){ 		0, 		0, 		0,		3}),	LANG("Throttle %", "��� %"),	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.Motor.BrakeFactor,			((LCP_Uint32_t){			0, 		0, 		0,		3}),	LANG("Brake %", "������ %"), 	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.Motor.SpeedRequest,			((LCP_Uint32_t){ 		0, 		0, 		0,		0}),	LANG("Speed request", "������ ��������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.Motor.Itorque_request,			((LCP_Uint32_t){		0, 		0, 		0,		1}),	LANG("Torque request", "������ ����"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.System.AmpLimAbs,			((LCP_Uint32_t){ 					0, 		0, 		0,		1}),	LANG("Torque limit", "���. ����� ����"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.System.TempCPU,			((LCP_Uint32_t){					0, 		0, 		0,		1}),	LANG("Temp CPU", "����������� CPU"), "%s �C"),
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
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Logger.Start, 			LANG("Start logging", "��������� ������"), 0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,RD.Logger.Stop, 			LANG("Stop logging", "���������� ������"), 0),
					 { pti(RD.Logger.State, 						0, 		1, 		1,		0),	PT_enum | PT_readonly,	LANG("# State", "# ���������"), LANG("Off\nRecording\nError\nStopped\nWaiting", "��������\n������\n������\n����������\n��������")}, //
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Logger.Error,			((LCP_Uint32_t){ 						0, 		1, 		1,		0}),	LANG("# Error code", "# ��� ������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Logger.StartMode, 			((LCP_Enum_t){0, Logger_StartMode_MAX - 1}),			LANG("Start mode", "����� �������"), LANG("Manual\nAt start", "������\n��� ���������")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Logger.Time, 			((LCP_Enum_t){0, 1}),			LANG("Log time", "����� � ����"), LANG("Sys time\nTime step", "���������\n��� �������")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Logger.Rate, 			((LCP_Enum_t){0, LogRate_MAX - 1}),			LANG("Log rate", "������ ������"), logratetext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Logger.Mode, 			((LCP_Enum_t){0, 1}),			LANG("Mode", "����� ������"), LANG("Buffered\nMax rate", "������\n���� ��������")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Logger.Filter, 			((LCP_Enum_t){0, LogFilter_MAX - 1}),			LANG("Data averaging", "���������� ������"), LANG("None\nFast\nSlow","���\n�����\n����")),
					 { 0,							0, 			5500, 	10, 	0,		0,	PT_value | PT_readonly, LANG("# Data to log:", "# ������ ��� ������:"), " " }, //
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
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		HWConfig.Limits.SupplyMaxV,			((LCP_Uint32_t){ 			0, 		1, 		1,		1}),	LANG("Max supply", "������ ����������"),	 "%sV"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		HWConfig.Limits.PhaseMaxA,			((LCP_Uint32_t){ 			0, 		1, 		1,		1}),	LANG("Max current", "������������ ���"), "%s�"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		dummy,			((LCP_Uint32_t){ 								0, 		1, 		1,		0}),	LANG("Firmware date", "���� ��������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		dummy,			((LCP_Uint32_t){ 								0, 		1, 		1,		0}),	LANG("Firmware ver.", "������ ��������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		dummy,			((LCP_Uint32_t){ 								0, 		1, 		1,		0}),	LANG("Loader date", "���� ����������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		dummy,			((LCP_Uint32_t){ 								0, 		1, 		1,		0}),	LANG("Loader version", "������ ����������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		LifeData.TotalkWH,			((LCP_Uint32_t){ 					0, 		1, 		1,		0}),	LANG("Worked", "���������"), "%skW*h"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		LifeData.MiddleTemp100hr,			((LCP_Uint32_t){ 			0, 		1, 		1,		1}),	LANG("t� middle 100h", "t� ����. �� 100�"), "%s�C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		LifeData.MiddleTemp,			((LCP_Uint32_t){ 					0, 		1, 		1,		1}),	LANG("t� middle", "t� ����."), "%s�C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		LifeData.OVCRCounter,			((LCP_Uint32_t){ 				0, 		1, 		1,		0}),	LANG("Current protections", "����� �� ����"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		LifeData.OVTCounter,			((LCP_Uint32_t){ 					0, 		1, 		1,		0}),	LANG("Temperature protections", "����� �� �����������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		LifeData.OVVCounter,			((LCP_Uint32_t){ 					0, 		1, 		1,		0}),	LANG("Voltage protections", "����� �� ����������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		LifeData.PowerCycleCount,			((LCP_Uint32_t){ 			0, 		1, 		1,		0}),	LANG("Power cycle", "���������"), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Lifetime.Min,			((LCP_Uint32_t){ 						0, 		1, 		1,		0}),	LANG("Power-on time", "����� ������"), LANG("%s min", "%s ���.")),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Lifetime.Hours,			((LCP_Uint32_t){ 					0, 		1, 		1,		0}),	"-", 	LANG("%s h", "%s �.")),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Lifetime.Days,			((LCP_Uint32_t){ 					0, 		1, 		1,		0}),	"--", 	LANG("%s days", "%s ��.")),
};

const                LCPS_Entry_t PD_Misc[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Misc.EnableMode, 			((LCP_Enum_t){0, 3}),	LANG("Disable button", "������ ���������"), LANG("None\nSwitch\nButton\nCAN", "���������\n������.\n������\nCAN")),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.Misc.AutoShutdown , 	LANG("Auto shutdown", "��������������"),0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Misc.ShutdownTime,			((LCP_Uint32_t){ 			30, 	1500, 	5,		0}),	LANG("Sleep time", "����� ���"), "%s s"),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.Misc.LockAtEnable, 	LANG("Lock at turn-on", "���������� ��� ���."),	0),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.Misc.SpeedCalc, 	LANG("Speed calculation", "������ ��������"),	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Misc.SpeedCircLen_mm,			((LCP_Uint32_t){ 			0, 		3000, 	5,		0}),	LANG("Circumference", "����� ����������"), LANG("%s mm", "%s mm")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Misc.MotorSprocket,			((LCP_Uint32_t){ 			1, 		5000, 	1,		0}),	LANG("Motor sprocket", "������ ������"), LANG("%s t", "%s ���.")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Misc.WheelSprocket,			((LCP_Uint32_t){ 			1, 		5000, 	1,		0}),	LANG("Wheel sprocket", "������ ������"), LANG("%s t", "%s ���.")),
					 pbool(LCP_AccessLvl_Any, 	LCP_Normal,Config.Misc.Master, 	LANG("Master-controller", "������-����������"),	0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Misc.CPUtLim,			((LCP_Uint32_t){ 					60, 	105, 	5,		0}),	LANG("Limit t� CPU", "����� t� CPU"), "%s�C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Data.NodeID,			((LCP_Uint32_t){ 					0, 	LC_Null_Address - 1, 	1,		0}),	LANG("Device ID", "ID ����������"),	0),
};

const                LCPS_Entry_t PD_PortState[] = {
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.Enable,			((LCP_Uint32_t){ 					0, 		0, 		1, 		0}),	LANG("Enable button", "������ ���."), 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.S1,			((LCP_Uint32_t){ 						0, 		0, 		1, 		0}),	"S1", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.S3,			((LCP_Uint32_t){ 						0, 		0, 		1, 		0}),	"S3", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.RV,			((LCP_Uint32_t){ 						0, 		0, 		1, 		0}),	"RV", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.CR,			((LCP_Uint32_t){ 						0, 		0, 		1, 		0}),	"CR", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.P1,			((LCP_Uint32_t){ 						0, 		0, 		1, 		0}),	"P1", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.P2,			((LCP_Uint32_t){ 						0, 		0, 		1, 		0}),	"P2", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.P,			((LCP_Uint32_t){ 							0, 		0, 		1, 		0}),	"P", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Inputs.M,			((LCP_Uint32_t){ 							0, 		0, 		1, 		0}),	"M", 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.Embedd.VThrottle,			((LCP_Uint32_t){ 			0, 		0, 		0, 		3}), LANG("# Throttle","# ����� ����"),"%s V"),
					 pstd(LCP_AccessLvl_Any, 	LCP_ReadOnly,		RD.Control.Embedd.VBrake,			((LCP_Uint32_t){				0, 		0, 		0, 		3}), LANG("# Brake", "# ����� �������"),"%s V"),
};
/*
					 PF_CruiseInc,
					 PF_CruiseDec,
					 PF_CruiseRestore,*/
const                char buttext[] = "OFF\nRV\nCRe\nCR+\nCR-\nCRr\nBK\nDM\nDTH\nDPAS\nSWSNS\nN\nnBK\nS1\nS2\nS3\nS1of3\nS3of3\nScyc\nS++\nS--\nSPSNS\nSpec.";
const                LCPS_Entry_t PD_PortConfig[] = {
					 folder(LCP_AccessLvl_Any, 	Dir_PortState, 					0, 0),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.SpeedMode, 			((LCP_Enum_t){0, 1}),	LANG("Speeds mode", "����� ���������"), LANG("Switch\nButtons", "������.\n������")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.S1, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("Port S1", "���� S1"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.S3, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("Port S3", "���� S3"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.RV, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("Port RV", "���� RV"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.CR, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("Port CR", "���� CR"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.P1, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("Port P1", "���� P1"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.P2, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("Port P2", "���� P2"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.P, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("Port P", "���� P"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.M, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("Port M", "���� M"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT1, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 1", "CAN ���� 1"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT2, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 2", "CAN ���� 2"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT3, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 3", "CAN ���� 3"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT4, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 4", "CAN ���� 4"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT5, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 5", "CAN ���� 5"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT6, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 6", "CAN ���� 6"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT7, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 7", "CAN ���� 7"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT8, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 8", "CAN ���� 8"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT9, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 9", "CAN ���� 9"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT10, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 10", "CAN ���� 10"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT11, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 11", "CAN ���� 11"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT12, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 12", "CAN ���� 12"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT13, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 13", "CAN ���� 13"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT14, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 14", "CAN ���� 14"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT15, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 15", "CAN ���� 15"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.EXT16, 			((LCP_Enum_t){0, PF_Size - 1}),	LANG("CAN Port 16", "CAN ���� 16"), buttext),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM_Freq, 			((LCP_Enum_t){0, 5}),	LANG("PWM freq", "����� ���"), "100Hz\n500Hz\n1kHz\n5kHz\n10kHz\n25kHz(FAN)"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM1, 			((LCP_Enum_t){0, 4}),	LANG("PWM P1", "��� P1"), LANG("Disabled\nStop-light\nHeadligth\nt�C motor\nt�C controller\nPort busy!", "��������\n����-����\n��������\nt�C ������\nt�C �����������\n���� �����!")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM1Min,			((LCP_Uint32_t){ 			0, 		100, 		1,		0}),	LANG("PWM P1 Min", "��� P1 �������"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM1Max,			((LCP_Uint32_t){  			0, 		100, 		1,		0}),	LANG("PWM P1 Max", "��� P1 ��������"), "%s%%"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM1TempMin,			((LCP_Uint32_t){ 		0, 		100, 		1,		0}),	LANG("PWM P1 t� Min", "��� P1 t� �������"), "%s�C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM1TempMax,			((LCP_Uint32_t){  		0, 		100, 		1,		0}),	LANG("PWM P1 t� Max", "��� P1 t� ��������"), "%s�C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM2, 			((LCP_Enum_t){0, 4}),	LANG("PWM P2", "��� P2"), LANG("Disabled\nStop-light\nHeadligth\nt�C motor\nt�C controller\nPort busy!", "��������\n����-����\n��������\nt�C ������\nt�C �����������\n���� �����!")),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM2Min,			((LCP_Uint32_t){ 			0, 		100, 		1,		0}),	LANG("PWM P2 Min", "��� P2 �������"), "%s�C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM2Max,			((LCP_Uint32_t){  			0, 		100, 		1,		0}),	LANG("PWM P2 Max", "��� P2 ��������"), "%s�C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM2TempMin,			((LCP_Uint32_t){ 		0, 		100, 		1,		0}),	LANG("PWM P2 t� Min", "��� P2 t� �������"), "%s�C"),
					 pstd(LCP_AccessLvl_Any, 	LCP_Normal,		Config.Ports.PWM2TempMax,			((LCP_Uint32_t){  		0, 		100, 		1,		0}),	LANG("PWM P2 t� Max", "��� P2 t� ��������"), "%s�C"),
};

extern               const LCPS_Directory_t pDirectories[] = { //
					 directory(PD_Root,				0, LCP_AccessLvl_Any, "Controller"),
					 directory(PD_Autoconf,			0, LCP_AccessLvl_Any, LANG("Auto-setup", "�������������")),
					 directory(PD_Controls,			0, LCP_AccessLvl_Any, LANG("Control", "����������")),
					 directory(PD_ControlModes,		0, LCP_AccessLvl_Any, LANG("Control modes", "������ ����������")),
					 directory(PD_AdvancedModes,		0, LCP_AccessLvl_Any, LANG("Advanced modes", "�������������� ������")),
					 directory(PD_Advanced,			0, LCP_AccessLvl_Any, LANG("Mode S1", "����� S1")),
					 directory(PD_Advanced,			1, LCP_AccessLvl_Any, LANG("Mode S2", "����� S2")),
					 directory(PD_Advanced,			2, LCP_AccessLvl_Any, LANG("Mode S3", "����� S3")),
					 directory(PD_PID,				0, LCP_AccessLvl_Any, LANG("PID regulators", "PID ����������")),
					 directory(PD_Motor,				0, LCP_AccessLvl_Any, LANG("Motor setup", "��������� ������")),
					 directory(PD_MotorTsensor,		0, LCP_AccessLvl_Any, LANG("Motor t�-sensor", "����������� ������")),
					 directory(PD_MotorHallTable,	0, LCP_AccessLvl_Any, LANG("Hall table", "������� ������")),
					 directory(PD_Battery,			0, LCP_AccessLvl_Any, LANG("Battery", "�������")),
					 directory(PD_DCDC,				0, LCP_AccessLvl_Any, LANG("Converter", "���������������")),
					 directory(PD_flags,				0, LCP_AccessLvl_Any, LANG("Status flags", "����� �������")),
					 directory(PD_Clutch,			0, LCP_AccessLvl_Any, LANG("Clutch", "�����")),
					 directory(PD_About,				0, LCP_AccessLvl_Any, LANG("Device information", "���������� �� ����������")),
					 directory(PD_Tune,				0, LCP_AccessLvl_Any, LANG("Manual angle setup", "������ ��������� ����")),
					 directory(PD_DebugInfo,			0, LCP_AccessLvl_Any, LANG("Debug information", "���������� ����������")),
					 directory(PD_Misc,				0, LCP_AccessLvl_Any, LANG("Extra parameters", "���. ���������")),
					 directory(PD_PortState,			0, LCP_AccessLvl_Any, LANG("Port state", "��������� ������")),
					 directory(PD_PortConfig,		0, LCP_AccessLvl_Any, LANG("I/O configuration", "��������� ������")),
					 directory(PD_PAS,				0, LCP_AccessLvl_Any, "Pedal Assist System"),
					 directory(PD_Debug,				0, LCP_AccessLvl_Dev, "Debug"),
					 directory(PD_Menu,				0, LCP_AccessLvl_Any, LANG("Updates and settings", "��������� � ����������")),
					 directory(PD_DebugFoc,			0, LCP_AccessLvl_Any, LANG("Debug FOC", "������� FOC")),
					 directory(PD_DebugRemote,		0, LCP_AccessLvl_Any, LANG("Remote inputs", "��������� ����������")),
					 directory(PD_Logger,			0, LCP_AccessLvl_Any, LANG("Logger", "������")),
					 directory(PD_RCPWM,				0, LCP_AccessLvl_Any, LANG("RC Control", "RC ����������")),
					 directory(PD_Cruise,			0, LCP_AccessLvl_Any, LANG("Cruise", "�����")),
					 directory(PD_Curves,			0, LCP_AccessLvl_Any, LANG("Throttle/brake curves", "������ ����� ����/�������"))
};
//                   @formatter:on
extern               const int dirsize = ARRAYSIZ(pDirectories);
