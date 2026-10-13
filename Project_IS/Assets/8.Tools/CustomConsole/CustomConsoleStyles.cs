using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomConsole
{
    public static class CustomConsoleStyles
    {
        public static readonly Color color_frame = new Color(0.16f, 0.16f, 0.16f);

        public static readonly Color color_executionOrder = new Color(0.19f, 0.19f, 0.19f);

        public static readonly Color color_log_A = new Color(0.215f, 0.215f, 0.215f);

        public static readonly Color color_log_B = new Color(0.235f, 0.235f, 0.235f);

        public static readonly Color color_selectedLog = new Color(.25f, .45f, .70f, .8f);

        public static readonly Color color_frameSeparator = new Color(0.35f, 0.35f, 0.35f);

        public static readonly Color color_executionOrderSeparator = new Color(0.27f, 0.27f, 0.27f);

        public static readonly Color color_text_normal = new Color(0.35f, 0.65f, 1f);

        public static readonly Color color_text_hover = new Color(0.55f, 0.8f, 1f);

        public const float HEIGHT_TEXT_DETAIL = 15f;
        public const float FRAME_SEPARATER_SIZE = 2f;
        public const float EXECUTION_ORDER_SEPARATER_SIZE = 1f;
    }
}