using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XIVRusUpdater.Utils;

public sealed class DynamicRowValue(Func<IDataManager, IEnumerable<uint>> resolver)
{
    public IEnumerable<uint> Resolve(IDataManager dataManager) => resolver(dataManager);
}

public static class DynamicRow
{
    public static readonly DynamicRowValue TriadResident = new(GetTriadNpc);

    private static IEnumerable<uint> GetTriadNpc(IDataManager dataManager)
    {
        var triadNpcs = dataManager.GetExcelSheet<TripleTriad>().Select(r => r.RowId).Where(i => i != 0).ToHashSet();
        return dataManager.GetExcelSheet<ENpcBase>().Where(r => r.ENpcData.Any(d => triadNpcs.Contains(d.RowId))).Select(r => r.RowId).OrderBy(i => i);
    }
};
