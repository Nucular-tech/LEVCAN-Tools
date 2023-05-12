using ImGuiNET;
using LEVCAN_Configurator;
using Newtonsoft.Json.Linq;
using SharpGen.Runtime.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;


namespace ImGuiNET
{
    partial class ImGuiNative2
    {
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern bool igDragBehavior(uint id, ImGuiDataType data_type, void* p_v, float v_speed, void* p_min, void* p_max, byte* format, ImGuiSliderFlags flags);

        static Regex patternSimple = new Regex(@"%(?:l{0,2}|h{0,2}|[jzt]{0,1})[diouxXeEfFgGaAcpsn]");
        static Regex pattern = new Regex(@"%[+\-0-9]*\.*([0-9]*)([xXeEfFdDgG])");

        public static void Text(string format, object arg)
        {
            ImGui.Text(FormatText(format, arg));
        }

        public static string FormatText(string format, object arg)
        {
            //prepare for regex
            string csharpformat = format.Replace("%%", "%").Replace("{", "{{").Replace("}", "}}");
            //replace complex %.2f
            string newFormat = pattern.Replace(csharpformat, m =>
            {
                if (m.Groups.Count == 3)
                {
                    return "{0:" + m.Groups[2].Value + m.Groups[1].Value + "}";

                }
                return "{0}";
            });
            newFormat = patternSimple.Replace(newFormat, m => "{0}");
            string output = String.Format(newFormat, arg);
            return output;
        }
    }
}

namespace ImGuiKnobs
{
    [Flags]
    public enum ImGuiKnobFlags
    {
        NoTitle = 1 << 0,
        NoInput = 1 << 1,
        ValueTooltip = 1 << 2,
        DragHorizontal = 1 << 3,
        BottomTitle = 1 << 4,
        CenterValue = 1 << 5,
        NoHover = 1 << 6,
    }

    public enum ImGuiKnobVariant
    {
        Tick = 1 << 0,
        Dot = 1 << 1,
        Wiper = 1 << 2,
        WiperOnly = 1 << 3,
        WiperDot = 1 << 4,
        Stepped = 1 << 5,
        Space = 1 << 6,
    }

    public class Knob
    {
        public float radius;
        public bool value_changed;
        public Vector2 center;
        public bool is_active;
        public bool is_hovered;
        public float angle_min;
        public float angle_max;
        public float t;
        public float angle;
        public float angle_cos;
        public float angle_sin;

        const int ImGuiSliderFlags_Vertical = 1 << 20;
        ImGuiKnobVariant knobVariant;
        ImGuiKnobFlags knobFlags;
        ImGuiDataType knobDataType;
        float linethickness;
        float thicknessBackground;
        float knobSize;
        int knobSteps;

        public Knob(ImGuiDataType data_type, ImGuiKnobVariant variant, float size, ImGuiKnobFlags flags, float thickness, float thicknessBG, float angleMin = 0.75f, float angleMax = 2.25f, int knobSteps = 10)
        {
            knobVariant = variant;
            knobFlags = flags;
            linethickness = thickness;
            thicknessBackground = thicknessBG;
            knobSize = size;
            knobDataType = data_type;
            angle_min = (float)Math.PI * angleMin;
            angle_max = (float)Math.PI * angleMax;
            this.knobSteps = knobSteps;
        }

        unsafe void knobDrag(string label, object p_value, object v_min, object v_max, float speed, float radius, string format)
        {

            var screen_pos = ImGui.GetCursorScreenPos();

            // Handle dragging
            ImGui.InvisibleButton(label, new Vector2(radius * 2.0f, radius * 2.0f));
            var gid = ImGui.GetID(label);
            ImGuiSliderFlags drag_flags = 0;
            if ((knobFlags & ImGuiKnobFlags.DragHorizontal) == 0)
            {
                drag_flags |= (ImGuiSliderFlags)ImGuiSliderFlags_Vertical;
            }

            var num = Encoding.UTF8.GetByteCount(format);
            var ptrbyte = stackalloc byte[(int)(uint)(num + 1)];
            int utf = 0;
            fixed (char* ptr = format)
            {
                char* chars = ptr;
                utf = Encoding.UTF8.GetBytes(chars, format.Length, ptrbyte, num);
            }
            ptrbyte[utf] = 0;

            value_changed = ImGuiNative2.igDragBehavior(gid, knobDataType, &p_value, speed, &v_min, &v_max, ptrbyte, drag_flags);
            center = new Vector2(screen_pos.X + radius, screen_pos.Y + radius);
        }

        void draw_arc1(Vector2 center, float radius, float start_angle, float end_angle, float thickness, Vector4 color, int num_segments)
        {
            Vector2 start = new Vector2
            (
                center.X + (float)Math.Cos(start_angle) * radius,
                center.Y + (float)Math.Sin(start_angle) * radius
            );

            Vector2 end = new Vector2
            (
                center.X + (float)Math.Cos(end_angle) * radius,
                center.Y + (float)Math.Sin(end_angle) * radius
            );

            // Calculate bezier arc points
            float ax = start.X - center.X;
            float ay = start.Y - center.Y;
            float bx = end.X - center.X;
            float by = end.Y - center.Y;
            float q1 = ax * ax + ay * ay;
            float q2 = q1 + ax * bx + ay * by;
            float k2 = (4.0f / 3.0f) * ((float)Math.Sqrt((2.0f * q1 * q2)) - q2) / (ax * by - ay * bx);
            Vector2 arc1 = new Vector2(center.X + ax - k2 * ay, center.Y + ay + k2 * ax);
            Vector2 arc2 = new Vector2(center.X + bx + k2 * by, center.Y + by - k2 * bx);

            var draw_list = ImGui.GetWindowDrawList();
            draw_list.AddBezierCubic(start, arc1, arc2, end, ImGui.GetColorU32(color), thickness, num_segments);
        }

        void draw_arc(Vector2 center, float radius, float start_angle, float end_angle, float thickness, Vector4 color, int num_segments, int bezier_count)
        {
            // Overlap and angle of ends of bezier curves needs work, only looks good when not transperant
            float overlap = thickness * radius * 0.00001f * (float)Math.PI;
            float delta = end_angle - start_angle;
            float bez_step = 1.0f / bezier_count;
            float mid_angle = start_angle + overlap;

            for (int i = 0; i < bezier_count - 1; i++)
            {
                float mid_angle2 = delta * bez_step + mid_angle;
                draw_arc1(center, radius, mid_angle - overlap, mid_angle2 + overlap, thickness, color, num_segments);
                mid_angle = mid_angle2;
            }

            draw_arc1(center, radius, mid_angle - overlap, end_angle, thickness, color, num_segments);
        }

        void draw_dot(float size, float radius, float angle, color_set color, bool filled, int segments)
        {
            var dot_size = size * this.radius;
            var dot_radius = radius * this.radius;

            ImGui.GetWindowDrawList().AddCircleFilled(
                new Vector2(center[0] + (float)Math.Cos(angle) * dot_radius, center[1] + (float)Math.Sin(angle) * dot_radius),
                dot_size,
                ImGui.GetColorU32(is_active ? color.active : (is_hovered ? color.hovered : color.basic)),
                segments);
        }

        void draw_tick(float start, float end, float width, float angle, color_set color)
        {
            var tick_start = start * radius;
            var tick_end = end * radius;
            var angle_cos = (float)Math.Cos(angle);
            var angle_sin = (float)Math.Sin(angle);


            ImGui.GetWindowDrawList().AddLine(
                new Vector2(center[0] + angle_cos * tick_end, center[1] + angle_sin * tick_end),
                new Vector2(center[0] + angle_cos * tick_start, center[1] + angle_sin * tick_start),
                ImGui.GetColorU32(is_active ? color.active : (is_hovered ? color.hovered : color.basic)),
                width * radius);
        }

        void draw_circle(float size, color_set color, bool filled, int segments)
        {
            var circle_radius = size * radius;

            ImGui.GetWindowDrawList().AddCircleFilled(
                center,
                circle_radius,
                ImGui.GetColorU32(is_active ? color.active : (is_hovered ? color.hovered : color.basic)),
                segments);
        }

        void draw_arc(float radius, float size, float start_angle, float end_angle, color_set color, int segments, int bezier_count)
        {
            var track_radius = radius * this.radius;
            var track_size = size * this.radius * 0.5f + 0.0001f;

            draw_arc(
                    center,
                    track_radius,
                    start_angle,
                    end_angle,
                    track_size,
                    is_active ? color.active : (is_hovered ? color.hovered : color.basic),
            segments,
            bezier_count);
        }

        public bool DrawKnob(string label, ref object p_value, object v_min, object v_max, float change_speed, string format)
        {
            float min = Convert.ToSingle(v_min);
            float max = Convert.ToSingle(v_max);
            float value = Convert.ToSingle(p_value);

            if (value < min)
                value = min;
            if (value > max)
                value = max;
            var speed = change_speed == 0 ? min - max / 250f : change_speed;

            ImGui.PushID(label);
            var width = knobSize == 0 ? ImGui.GetTextLineHeight() * 4.0f : knobSize * ImGui.GetIO().FontGlobalScale;
            ImGui.PushItemWidth(width);
            ImGui.BeginGroup();

            var xy_start = ImGui.GetCursorPos();

            // Draw title
            if (!(knobFlags.HasFlag(ImGuiKnobFlags.NoTitle)))
            {
                var title_size = ImGui.CalcTextSize(label, false, width);
                if (knobFlags.HasFlag(ImGuiKnobFlags.BottomTitle))
                {
                    //Bottom title
                    ImGui.SetCursorPosX(xy_start.X + (width - title_size.X) * 0.5f);
                    ImGui.SetCursorPosY(xy_start.Y + width - title_size.Y * 1.0f);
                    ImGui.Text(label);
                    ImGui.SetCursorPosY(xy_start.Y);
                }
                else
                {
                    // Center title
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (width - title_size.X) * 0.5f);
                    ImGui.Text(label);
                }
            }

            radius = width * 0.5f;

            t = (value - min) / (max - min);
            // Prepare knob
            knobDrag(label, p_value, v_min, v_max, speed, radius, format);
            is_active = ImGui.IsItemActive();
            if (!knobFlags.HasFlag(ImGuiKnobFlags.NoHover))
                is_hovered = ImGui.IsItemHovered();
            else
                is_hovered = false;

            angle = angle_min + (angle_max - angle_min) * t;
            angle_cos = (float)Math.Cos(angle);
            angle_sin = (float)Math.Sin(angle);

            // Draw tooltip
            if (knobFlags.HasFlag(ImGuiKnobFlags.ValueTooltip) && (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) || ImGui.IsItemActive()))
            {
                ImGui.BeginTooltip();
                ImGuiNative2.Text(format, p_value); //todo fix
                ImGui.EndTooltip();
            }

            // Draw input
            if (!(knobFlags.HasFlag(ImGuiKnobFlags.NoInput)))
            {
                ImGuiSliderFlags drag_flags = 0;
                if (!(knobFlags.HasFlag(ImGuiKnobFlags.DragHorizontal)))
                {
                    drag_flags |= (ImGuiSliderFlags)ImGuiSliderFlags_Vertical;
                }
                GCHandle handle = GCHandle.Alloc(p_value, GCHandleType.Pinned);
                IntPtr valueptr = handle.AddrOfPinnedObject();

                GCHandle handlemin = GCHandle.Alloc(v_min, GCHandleType.Pinned);
                IntPtr minptr = handle.AddrOfPinnedObject();

                GCHandle handlemax = GCHandle.Alloc(v_max, GCHandleType.Pinned);
                IntPtr maxptr = handle.AddrOfPinnedObject();

                var changed = ImGui.DragScalar("###knob_drag", knobDataType, valueptr, speed, minptr, maxptr, format, drag_flags);

                handle.Free();
                handlemin.Free();
                handlemax.Free();

                if (changed)
                {
                    value_changed = true;
                }
            }

            DrawBar();

            // Draw central value
            if (knobFlags.HasFlag(ImGuiKnobFlags.CenterValue))
            {
                string formatted = ImGuiNative2.FormatText(format, p_value);
                var value_size = ImGui.CalcTextSize(formatted, false, width);
                var xy_bottom = ImGui.GetCursorPos();
                ImGui.SetCursorPosY(xy_start.Y + (width - value_size.Y) * 0.5f);
                ImGui.SetCursorPosX(xy_start.X + (width - value_size.X) * 0.5f);
                ImGui.Text(formatted);
                ImGui.SetCursorPos(xy_bottom);
            }
            ImGui.EndGroup();
            ImGui.PopItemWidth();
            ImGui.PopID();

            return value_changed;
        }

        void DrawBar()
        {
            switch (knobVariant)
            {
                case ImGuiKnobVariant.Tick:
                    {
                        draw_circle(0.85f, color_set.GetSecondaryColorSet(), true, 32);
                        draw_tick(0.5f, 0.85f, 0.08f, angle, color_set.GetPrimaryColorSet());
                        break;
                    }
                case ImGuiKnobVariant.Dot:
                    {
                        draw_circle(0.85f, color_set.GetSecondaryColorSet(), true, 32);
                        draw_dot(0.12f, 0.6f, angle, color_set.GetPrimaryColorSet(), true, 12);
                        break;
                    }

                case ImGuiKnobVariant.Wiper:
                    {
                        draw_circle(0.7f, color_set.GetSecondaryColorSet(), true, 32);
                        draw_arc(0.8f, thicknessBackground, angle_min, angle_max, color_set.GetTrackColorSet(), 16, 2);

                        if (t > 0.01f)
                        {
                            draw_arc(0.8f, linethickness, angle_min, angle, color_set.GetPrimaryColorSet(), 16, 2);
                        }
                        break;
                    }
                case ImGuiKnobVariant.WiperOnly:
                    {
                        draw_arc(0.8f, thicknessBackground, angle_min, angle_max, color_set.GetTrackColorSet(), 32, 2);

                        if (t > 0.01f)
                        {
                            draw_arc(0.8f, linethickness, angle_min, angle, color_set.GetPrimaryColorSet(), 16, 2);
                        }
                        break;
                    }
                case ImGuiKnobVariant.WiperDot:
                    {
                        draw_circle(0.6f, color_set.GetSecondaryColorSet(), true, 32);
                        draw_arc(0.85f, linethickness, angle_min, angle_max, color_set.GetTrackColorSet(), 16, 2);
                        draw_dot(0.1f, 0.85f, angle, color_set.GetPrimaryColorSet(), true, 12);
                        break;
                    }
                case ImGuiKnobVariant.Stepped:
                    {
                        for (float n = 0; n < knobSteps; n++)
                        {
                            var a = n / (knobSteps - 1);
                            var angle = angle_min + (angle_max - angle_min) * a;
                            draw_tick(0.7f, 0.9f, 0.04f, angle, color_set.GetPrimaryColorSet());
                        }

                        draw_circle(0.6f, color_set.GetSecondaryColorSet(), true, 32);
                        draw_dot(0.12f, 0.4f, angle, color_set.GetPrimaryColorSet(), true, 12);
                        break;
                    }
                case ImGuiKnobVariant.Space:
                    {
                        draw_circle(0.3f - t * 0.1f, color_set.GetSecondaryColorSet(), true, 16);

                        if (t > 0.01f)
                        {
                            draw_arc(0.4f, 0.15f, angle_min - 1.0f, angle - 1.0f, color_set.GetPrimaryColorSet(), 16, 2);
                            draw_arc(0.6f, 0.15f, angle_min + 1.0f, angle + 1.0f, color_set.GetPrimaryColorSet(), 16, 2);
                            draw_arc(0.8f, 0.15f, angle_min + 3.0f, angle + 3.0f, color_set.GetPrimaryColorSet(), 16, 2);
                        }
                        break;
                    }

            }
        }

        public bool DrawValueKnob(string label, string value, float progress, string tooltip = null)
        {
            ImGui.PushID(label);
            var width = knobSize == 0 ? ImGui.GetTextLineHeight() * 4.0f : knobSize * ImGui.GetIO().FontGlobalScale;
            ImGui.PushItemWidth(width);
            ImGui.BeginGroup();

            var xy_start = ImGui.GetCursorPos();

            // Draw title
            if (!(knobFlags.HasFlag(ImGuiKnobFlags.NoTitle)))
            {
                var title_size = ImGui.CalcTextSize(label, false, width);
                if (knobFlags.HasFlag(ImGuiKnobFlags.BottomTitle))
                {
                    //Bottom title
                    ImGui.SetCursorPosX(xy_start.X + (width - title_size.X) * 0.5f);
                    ImGui.SetCursorPosY(xy_start.Y + width - title_size.Y * 1.0f);
                    ImGui.Text(label);
                    ImGui.SetCursorPosY(xy_start.Y);
                }
                else
                {
                    // Center title
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (width - title_size.X) * 0.5f);
                    ImGui.Text(label);
                }
            }

            radius = width * 0.5f;

            t = progress;
            // Prepare knob
            var screen_pos = ImGui.GetCursorScreenPos();
            center = new Vector2(screen_pos.X + radius, screen_pos.Y + radius);
            ImGui.InvisibleButton(label, new Vector2(radius * 2.0f, radius * 2.0f));

            is_active = false;
            is_hovered = false;

            angle = angle_min + (angle_max - angle_min) * t;
            angle_cos = (float)Math.Cos(angle);
            angle_sin = (float)Math.Sin(angle);

            // Draw tooltip
            if (tooltip != null)
            {
                ImGui.BeginTooltip();
                ImGui.Text(tooltip); //todo fix
                ImGui.EndTooltip();
            }

            DrawBar();

            // Draw central value
            if (knobFlags.HasFlag(ImGuiKnobFlags.CenterValue))
            {
                var fonts = ImGui.GetIO().Fonts.Fonts;
                bool extrafont = fonts.Size > 1;
                if (extrafont)
                    ImGui.PushFont(fonts[1]);
                var value_size = ImGui.CalcTextSize(value, false, width);
                var xy_bottom = ImGui.GetCursorPos();
                ImGui.SetCursorPosY(xy_start.Y + (width - value_size.Y) * 0.5f);
                ImGui.SetCursorPosX(xy_start.X + (width - value_size.X) * 0.5f);
                ImGui.Text(value);
                if (extrafont)
                    ImGui.PopFont();
                ImGui.SetCursorPos(xy_bottom);
            }
            ImGui.EndGroup();
            ImGui.PopItemWidth();
            ImGui.PopID();

            return value_changed;
        }

        public bool KnobFloat(string label, ref float p_value, float v_min, float v_max, float speed, string format)
        {
            knobDataType = ImGuiDataType.Float;
            string _format = format == null ? "%.3f" : format;
            object value = p_value;
            var did = DrawKnob(label, ref value, v_min, v_max, speed, _format);
            p_value = (float)value;
            return did;
        }

        public bool KnobInt(string label, ref int p_value, int v_min, int v_max, float speed, string format)
        {
            knobDataType = ImGuiDataType.S32;
            string _format = format == null ? "%i" : format;
            object value = p_value;
            var did = DrawKnob(label, ref value, v_min, v_max, speed, _format);
            p_value = (int)value;
            return did;
        }
    }

    public struct color_set
    {
        static Vector4 primary;

        public Vector4 basic;
        public Vector4 hovered;
        public Vector4 active;

        public static color_set GetPrimaryColorSet()
        {
            var colors = ImGui.GetStyle().Colors;

            return new color_set
            {
                basic = colors[(int)ImGuiCol.PlotHistogram],
                hovered = colors[(int)ImGuiCol.PlotHistogramHovered],
                active = colors[(int)ImGuiCol.PlotHistogramHovered]
            };
        }

        public static color_set GetSecondaryColorSet()
        {
            var colors = ImGui.GetStyle().Colors;
            var active = new Vector4(
                colors[(int)ImGuiCol.ButtonActive].X * 0.5f,
                colors[(int)ImGuiCol.ButtonActive].Y * 0.5f,
                colors[(int)ImGuiCol.ButtonActive].Z * 0.5f,
                colors[(int)ImGuiCol.ButtonActive].W);

            var hovered = new Vector4(
                colors[(int)ImGuiCol.ButtonHovered].X * 0.5f,
                colors[(int)ImGuiCol.ButtonHovered].Y * 0.5f,
                colors[(int)ImGuiCol.ButtonHovered].Z * 0.5f,
                colors[(int)ImGuiCol.ButtonHovered].W);

            return new color_set
            {
                basic = active,
                hovered = hovered,
                active = hovered
            };
        }

        public static color_set GetTrackColorSet()
        {
            var colors = ImGui.GetStyle().Colors;

            return new color_set
            {
                basic = colors[(int)ImGuiCol.FrameBg],
                hovered = colors[(int)ImGuiCol.FrameBg],
                active = colors[(int)ImGuiCol.FrameBg]
            };
        }
    };


}

