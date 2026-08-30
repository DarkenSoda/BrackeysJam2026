using System.Collections.Generic;
using UnityEngine;

public class RolledTip
{
    public TipGiver giver;
    public string line;
    public bool isTruth;
    public string newFact;
}

public class RolledTrip
{
    public string destination;
    public List<string> scratchpad = new List<string>();
    public string customerLine;
    public string dropoffLine;
    public Sprite passengerSprite;
    public List<RolledTip> tips = new List<RolledTip>();
}

public static class TripGenerator
{
    public static RolledTrip Roll(TaxiContent content)
    {
        var trip = new RolledTrip();

        var dests = Copy(content.destinations);
        var marks = Copy(content.landmarks);
        var dirs = Copy(content.directions);
        var givers = CopyGivers(content.tipGivers);

        trip.destination = Take(dests);

        int padCount = Mathf.Clamp(content.landmarksOnPad, 1, marks.Count);
        var knownMarks = new List<string>();
        for (int i = 0; i < padCount; i++)
            knownMarks.Add(Take(marks));

        trip.scratchpad.Add(trip.destination);
        trip.scratchpad.AddRange(knownMarks);

        string padA = knownMarks[0];
        string padB = knownMarks.Count > 1 ? knownMarks[1] : knownMarks[0];

        trip.customerLine = Fill(Pick(content.customerLines), trip.destination, padA, padB, "");
        trip.dropoffLine = Pick(content.dropoffLines);

        string trueDir = Pick(dirs);
        var wrongDirs = Copy(content.directions);
        wrongDirs.Remove(trueDir);

        int minTips = Mathf.Max(1, content.minTips);
        int maxTips = Mathf.Max(minTips, content.maxTips);
        int tipCount = Random.Range(minTips, maxTips + 1);
        if (givers.Count > 0)
            tipCount = Mathf.Min(tipCount, givers.Count);

        bool heardTrueDir = false;

        for (int i = 0; i < tipCount; i++)
        {
            TipGiver giver = givers.Count > 0 ? TakeGiver(givers) : null;
            float lieChance = giver != null ? giver.lieChance : 0.45f;
            bool lie = Random.value < lieChance;
            if (i == tipCount - 1 && Random.value < 0.6f)
                lie = false;

            string dir;
            string markA;
            string markB;
            string newFact = null;

            if (lie)
            {
                markB = Pick(knownMarks);
                if (heardTrueDir && wrongDirs.Count > 0)
                {
                    dir = Pick(wrongDirs);
                    markA = Pick(knownMarks);
                }
                else
                {
                    dir = wrongDirs.Count > 0 ? Pick(wrongDirs) : trueDir;
                    markA = marks.Count > 0 ? Pick(marks) : markB;
                }
            }
            else
            {
                dir = trueDir;
                markA = Pick(knownMarks);
                if (marks.Count > 0)
                {
                    markB = Take(marks);
                    newFact = markB;
                    knownMarks.Add(markB);
                }
                else
                {
                    markB = Pick(knownMarks);
                }
                heardTrueDir = true;
            }

            List<string> pool = null;
            if (giver != null)
                pool = lie ? giver.lieLines : giver.truthLines;

            trip.tips.Add(new RolledTip
            {
                giver = giver,
                isTruth = !lie,
                newFact = newFact,
                line = Fill(Pick(pool), trip.destination, markA, markB, dir)
            });
        }

        return trip;
    }

    static List<string> Copy(List<string> src)
    {
        return src != null ? new List<string>(src) : new List<string>();
    }

    static List<TipGiver> CopyGivers(List<TipGiver> src)
    {
        var list = new List<TipGiver>();
        if (src == null) return list;
        for (int i = 0; i < src.Count; i++)
        {
            if (src[i] != null)
                list.Add(src[i]);
        }
        return list;
    }

    static string Take(List<string> pool)
    {
        int i = Random.Range(0, pool.Count);
        string v = pool[i];
        pool.RemoveAt(i);
        return v;
    }

    static TipGiver TakeGiver(List<TipGiver> pool)
    {
        int i = Random.Range(0, pool.Count);
        TipGiver v = pool[i];
        pool.RemoveAt(i);
        return v;
    }

    static string Pick(List<string> pool)
    {
        if (pool == null || pool.Count == 0)
            return "";
        return pool[Random.Range(0, pool.Count)];
    }

    static string Fill(string template, string dest, string a, string b, string dir)
    {
        if (string.IsNullOrEmpty(template))
            return dest;

        return template
            .Replace("{dest}", dest)
            .Replace("{a}", a)
            .Replace("{b}", b)
            .Replace("{dir}", dir);
    }
}
