using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace FacilityEscape
{
    [RequireComponent(typeof(TextMesh))]
    public sealed class MultiEscapeClueBoard : MonoBehaviour
    {
        [SerializeField] private MultiEscapeGame game;
        [SerializeField, Range(8, 31)] private int clueLayer = 30;
        private TextMesh textMesh;

        private void Awake()
        {
            textMesh = GetComponent<TextMesh>();
            // 안내 문구 전용 오브젝트에 붙입니다. 벽 전체에 붙이면 벽도 A에게 사라집니다.
            foreach (var child in GetComponentsInChildren<Transform>(true)) child.gameObject.layer = clueLayer;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
        private void OnEnable() { if (game != null) game.onRoundStarted.AddListener(Refresh); }
        private void Start() => Refresh();
        private void OnDisable() { if (game != null) game.onRoundStarted.RemoveListener(Refresh); }

        public void Refresh()
        {
            if (game == null || game.Rules == null || textMesh == null) return;
            var rules = game.Rules;
            if (rules.Stage == EscapeStage.Ready)
            {
                textMesh.text = "WAITING FOR ROUND\n\nNEW ANSWERS ARE GENERATED AT START";
                return;
            }
            var builder = new StringBuilder("COLOR ORDER\n");
            for (int i = 0; i < rules.ColorCount; i++)
            {
                if (i > 0) builder.Append(" > ");
                builder.Append(rules.GetColorAnswer(i).ToString().ToUpperInvariant());
            }
            builder.Append("\n\nSYMBOL ORDER (LEFT > RIGHT)\n");
            for (int i = 0; i < MultiEscapeRules.DialCount; i++)
            {
                if (i > 0) builder.Append(" > ");
                builder.Append(SymbolText(rules.GetSymbolAnswer(i)));
            }
            builder.Append("\n\nCLOCKWISE: O > TRI > [] > * > <> > +");
            textMesh.text = builder.ToString();
        }

        // 기본 폰트에서도 누락되지 않는 기호 표기. 실제 기호 그림은 다이얼 메시/텍스처로 교체 가능합니다.
        public static string SymbolText(DialSymbol symbol)
        {
            switch (symbol)
            {
                case DialSymbol.Circle: return "O";
                case DialSymbol.Triangle: return "TRI";
                case DialSymbol.Square: return "[]";
                case DialSymbol.Star: return "*";
                case DialSymbol.Diamond: return "<>";
                case DialSymbol.Cross: return "+";
                default: return "?";
            }
        }
    }
}
