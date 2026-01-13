using UnityEngine;

public class NewsManager : MonoBehaviour
{
    public UINewsTicker ticker;

    [Header("Virus Highlight Options")]
    public Color virusColor = Color.red;
    public bool boldVirus = true;          // 굵게
    public bool bracketVirus = true;       // [바이러스명]

    // Stage 1~5 고정 뉴스
    readonly string[] earlyNews =
{
    // 1
    "Global travel and daily life continue normally across the world",

    // 2
    "{COUNTRY} reports record tourism numbers this season",

    // 3
    "Health officials in {COUNTRY} note a slight rise in seasonal flu cases",

    // 4
    "Local clinics in {COUNTRY} report increased patient visits",

    // 5
    "Medical professionals in {COUNTRY} begin monitoring unusual symptoms",

    // 6
    "Hospitals in {COUNTRY} observe a steady increase in respiratory cases",

    // 7
    "Public health agencies in {COUNTRY} issue precautionary guidelines",

    // 8
    "Authorities in {COUNTRY} acknowledge growing strain on hospitals",

    // 9
    "Unusual illness reports in {COUNTRY} begin attracting media attention",

    // 10
    "Medical experts in {COUNTRY} investigate unexplained infection patterns",

    // 11
    "Public concern grows as health services in {COUNTRY} face rising demand",

    // 12
    "Healthcare systems in {COUNTRY} report operational challenges",

    // 13
    "Emergency preparedness measures are discussed within {COUNTRY}",

    // 14
    "Officials in {COUNTRY} warn the situation may worsen without intervention",

    // 15
    "Hospitals across {COUNTRY} report critical capacity levels",

    // 16
    "Authorities in {COUNTRY} struggle to contain the spreading illness",

    // 17
    "Reports suggest large portions of {COUNTRY} are affected by the outbreak",

    // 18 🇺🇸 미국 멸망
    "The United States collapses as the {VIRUS} outbreak overwhelms all systems",

    // 19
    "Global shock follows the sudden collapse of the United States",

    // 20
    "Global markets and alliances destabilize in the wake of U.S. collapse",

    // 21
    "Multiple regions experience escalating unrest and system failures",

    // 22 🇻🇪 베네수엘라 멸망
    "Venezuela collapses amid uncontrollable spread of {VIRUS}",

    // 23
    "International aid efforts falter as crises multiply",

    // 24
    "Economic and humanitarian conditions worsen across affected nations",

    // 25
    "Global supply chains begin to fracture under mounting pressure",

    // 26
    "Governments worldwide report increasing internal instability",

    // 27
    "Public services in multiple countries experience severe disruption",

    // 28
    "Health agencies warn containment is rapidly failing",

    // 29
    "Widespread panic spreads as confidence in governments erodes",

    // 30
    "Experts warn the global situation is nearing a breaking point",

    // 31
    "Large-scale evacuations are reported across several regions",

    // 32
    "Medical infrastructure collapses in heavily affected areas",

    // 33
    "Global coordination efforts begin to break down",

    // 34
    "Remaining stable regions struggle to maintain order",

    // 35
    "Analysts warn the outbreak is entering an irreversible phase",

    // 36
    "Major powers show signs of internal collapse",

    // 37 🇨🇳 중국 멸망
    "China collapses as the {VIRUS} outbreak spirals beyond control",

    // 38
    "Global stability deteriorates sharply following the collapse of China",

    // 39
    "Worldwide emergency measures fail to restore order",

    // 40
    "Only a few regions remain functional amid global chaos",

    // 41
    "Experts warn the final unaffected areas are under imminent threat",

    // 42
    "The international system is described as effectively broken",

    // 43 🇬🇱 그린란드 멸망
    "Greenland collapses, marking the final failure to contain {VIRUS}"
};




    string ColorToHex(Color c)
    {
        return ColorUtility.ToHtmlStringRGB(c);
    }
    string FormatVirus(string virus)
    {
        // 혹시 모를 태그 깨짐 방지
        if (string.IsNullOrEmpty(virus))
            return string.Empty;
        virus = virus.Replace("<", "").Replace(">", "");

        if (bracketVirus)
            virus = $"[{virus}]";

        if (boldVirus)
            virus = $"<b>{virus}</b>";

        string hex = ColorToHex(virusColor);
        virus = $"<color=#{hex}>{virus}</color>";

        return virus;
    }

    string ApplyContext(string msg, string virus, string country)
    {
        string formattedVirus = FormatVirus(virus);

        return msg
            .Replace("{VIRUS}", formattedVirus)
            .Replace("{COUNTRY}", country);
    }

    public void PlayNews(int stage, string virus, string country, int percent)
    {
        Debug.Log("PlayNews: " + stage + ", " + virus + ", " + country + ", " + percent);
        string msg;

        // 🔹 Stage 1~5 : 고정 스토리 뉴스
        if (stage <= 46)
        {
            msg = earlyNews[stage - 1];
        }
        // 🔹 Stage 6+ : 동적 뉴스
        else
        {
            if (percent < 50)
                msg = $"{virus} spreads across {country}";
            else if (percent < 80)
                msg = $"Health alert issued in {country}";
            else
                msg = $"{country} reaches critical infection levels";
        }

        msg = ApplyContext(msg, virus, country);
        ticker.SetText(msg);
    }
}
