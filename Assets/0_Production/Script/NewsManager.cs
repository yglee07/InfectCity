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

    // 18
    "National response efforts in {COUNTRY} show signs of failure",

    // 19
    "Public services in {COUNTRY} begin experiencing widespread disruptions",

    // 20
    "Experts warn {COUNTRY} is approaching a point of systemic collapse",

    // 21 🇺🇸 미국 멸망
    "The United States collapses as the {VIRUS} outbreak overwhelms all systems",

    // 22
    "Global observers express concern following the collapse of the United States",

    // 23
    "Neighboring countries brace for impact as instability spreads",

    // 24
    "Economic and humanitarian crises deepen in affected regions",

    // 25 🇻🇪 베네수엘라 멸망
    "Venezuela collapses amid uncontrollable spread of {VIRUS}",

    // 26
    "International aid efforts struggle to respond to escalating crises",

    // 27
    "Multiple nations report growing unrest linked to the outbreak",

    // 28
    "Global supply chains show signs of severe disruption",

    // 29
    "Health agencies warn the situation is becoming increasingly unstable",

    // 30
    "Governments worldwide prepare for further national failures",

    // 31
    "The spread of {VIRUS} begins to destabilize major regions",

    // 32
    "Authorities warn that containment efforts are failing globally",

    // 33
    "Widespread panic emerges as multiple systems break down",

    // 34
    "Medical infrastructure collapses in several heavily affected areas",

    // 35
    "Experts fear the outbreak is entering an irreversible phase",

    // 36
    "Large-scale evacuations are reported across multiple regions",

    // 37
    "Global coordination efforts show signs of breakdown",

    // 38
    "The international community struggles to respond effectively",

    // 39
    "Warning signs emerge as major powers face internal collapse",

    // 40 🇨🇳 중국 멸망
    "China collapses as the {VIRUS} outbreak spirals beyond control",

    // 41
    "Global stability deteriorates following the collapse of China",

    // 42
    "Remaining governments face unprecedented pressure",

    // 43
    "Worldwide emergency measures fail to restore order",

    // 44
    "Only isolated regions remain functional amid global chaos",

    // 45
    "Experts warn the final unaffected regions are at imminent risk",

    // 46 🇬🇱 그린란드 멸망
    "Greenland collapses, marking the final failure to contain {VIRUS}"
};



    string ColorToHex(Color c)
    {
        return ColorUtility.ToHtmlStringRGB(c);
    }
    string FormatVirus(string virus)
    {
        // 혹시 모를 태그 깨짐 방지
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
