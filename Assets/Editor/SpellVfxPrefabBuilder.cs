using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 마블 능력별 전용 VFX 프리팹(SpellVfxComposite) 일괄 생성 + 각 SpellAbility.effectPrefab 자동 연결.
// 메뉴: Tools > Spell > Build Marble VFX Prefabs
// 재실행 안전: 기존 프리팹은 같은 경로에 덮어쓰기(GUID 유지) → 능력 에셋 참조 보존.
// 레이어 수치를 바꾸고 싶으면 아래 테이블 수정 후 다시 실행하거나, 생성된 프리팹을 인스펙터에서 직접 튜닝.
public static class SpellVfxPrefabBuilder
{
    const string PrefabDir = "Assets/Prefabs/Spell/Marble";

    [MenuItem("Tools/Spell/Build Marble VFX Prefabs")]
    public static void Build()
    {
        EnsureFolder();

        // 능력 클래스명 → 프리팹 (색은 각 능력의 vfxColor 테마와 일치)
        var map = new Dictionary<string, GameObject>();

        // ♥ 회복/버프
        map["HealAbility"] = MakePrefab("HealVFX", new[] {
            Flash(new Color(0.35f, 1f, 0.45f), 0.9f, 0.3f),
            Rise(new Color(0.35f, 1f, 0.45f), 0.5f, 18f, 0.18f),
        });
        map["SpeedUpAbility"] = MakePrefab("SpeedUpVFX", new[] {
            Flash(new Color(0.4f, 1f, 0.9f), 0.7f, 0.25f),
            Rise(new Color(0.4f, 1f, 0.9f), 0.35f, 16f, 0.16f),
        });
        map["LifestealAbility"] = MakePrefab("LifestealVFX", new[] {
            Flash(new Color(0.9f, 0.2f, 0.4f), 0.75f, 0.25f),
            Spark(new Color(0.9f, 0.2f, 0.4f), 0.9f, 10, 0.1f, 0.3f),
            Orbit(new Color(0.9f, 0.2f, 0.4f), 0.75f, 13f, 0.16f),
        });
        map["OverhealAbility"] = MakePrefab("OverhealVFX", new[] {
            Flash(new Color(1f, 0.85f, 0.35f), 0.85f, 0.3f),
            Rise(new Color(1f, 0.85f, 0.35f), 0.5f, 20f, 0.18f),
        });
        map["AdrenalineAbility"] = MakePrefab("AdrenalineVFX", new[] {
            Flash(new Color(1f, 0.55f, 0.15f), 0.8f, 0.25f),
            Burst(new Color(1f, 0.55f, 0.15f), 0.9f, 12, 0.12f, 0.3f),
            Orbit(new Color(1f, 0.55f, 0.15f), 0.75f, 14f, 0.16f),
            Rise(new Color(1f, 0.55f, 0.15f), 0.3f, 8f, 0.14f), // 아지랑이 열기
        });
        map["ResurrectionAbility"] = MakePrefab("ResurrectionVFX", new[] {
            Flash(new Color(1f, 0.95f, 0.5f), 1f, 0.35f),
            Rise(new Color(1f, 0.95f, 0.5f), 0.55f, 22f, 0.2f),
        });

        // ♦ 방어
        map["IronSkinAbility"] = MakePrefab("IronSkinVFX", new[] {
            Flash(new Color(0.75f, 0.75f, 0.8f), 0.85f, 0.3f),
            Implode(new Color(0.75f, 0.75f, 0.8f), 1.3f, 18, 0.12f, 0.35f), // 장갑 결집
            Orbit(new Color(0.75f, 0.75f, 0.8f), 0.75f, 12f, 0.16f),
        });
        map["ReflectAbility"] = MakePrefab("ReflectVFX", new[] {
            Flash(new Color(1f, 0.9f, 0.3f), 0.85f, 0.25f),
            Spark(new Color(1f, 0.9f, 0.3f), 1.1f, 14, 0.1f, 0.3f), // 가시 튐
            Orbit(new Color(1f, 0.9f, 0.3f), 0.8f, 13f, 0.16f),
        });
        map["CounterAbility"] = MakePrefab("CounterVFX", new[] {
            Flash(new Color(1f, 0.4f, 0.2f), 0.85f, 0.25f),
            Spark(new Color(1f, 0.4f, 0.2f), 1f, 12, 0.1f, 0.28f),
            Orbit(new Color(1f, 0.4f, 0.2f), 0.75f, 13f, 0.16f),
        });
        map["FortressAbility"] = MakePrefab("FortressVFX", new[] {
            Flash(new Color(0.6f, 0.6f, 1f), 1f, 0.35f),
            Implode(new Color(0.6f, 0.6f, 1f), 1.5f, 24, 0.13f, 0.4f), // 요새 형성
            Orbit(new Color(0.6f, 0.6f, 1f), 0.9f, 16f, 0.17f),
        });
        map["DashShieldAbility"] = MakePrefab("DashShieldVFX", new[] {
            Flash(new Color(0.4f, 0.8f, 1f), 0.75f, 0.25f),
            Orbit(new Color(0.4f, 0.8f, 1f), 0.7f, 13f, 0.15f),
        });
        map["MirrorWorldAbility"] = MakePrefab("MirrorWorldVFX", new[] {
            Flash(new Color(0.8f, 0.5f, 1f), 1.1f, 0.35f),
            Implode(new Color(0.8f, 0.5f, 1f), 1.6f, 22, 0.13f, 0.4f),
            Orbit(new Color(0.8f, 0.5f, 1f), 0.9f, 14f, 0.17f),
            Field(new Color(0.8f, 0.5f, 1f), 1f, 10f, 0.15f), // 공간 일렁임
        });
        map["SelfBuffShieldAbility"] = MakePrefab("ShieldVFX", new[] {
            Flash(new Color(0.3f, 0.8f, 1f), 0.85f, 0.3f),
            Orbit(new Color(0.3f, 0.8f, 1f), 0.8f, 14f, 0.16f),
        });

        // ♠ 총알 버프
        map["PiercingShotAbility"] = MakePrefab("PiercingShotVFX", new[] {
            Flash(new Color(1f, 0.95f, 0.6f), 0.75f, 0.25f),
            Spark(new Color(1f, 0.95f, 0.6f), 1f, 10, 0.1f, 0.3f),
            Orbit(new Color(1f, 0.95f, 0.6f), 0.7f, 12f, 0.15f),
        });
        map["SniperModeAbility"] = MakePrefab("SniperModeVFX", new[] {
            Flash(new Color(0.9f, 0.2f, 0.2f), 0.75f, 0.3f),
            Implode(new Color(0.9f, 0.2f, 0.2f), 1.2f, 16, 0.11f, 0.35f), // 조준 집중
            Orbit(new Color(0.9f, 0.2f, 0.2f), 0.7f, 12f, 0.15f),
        });
        map["ChainLightningAbility"] = MakePrefab("ChainLightningVFX", new[] {
            Flash(new Color(0.6f, 0.85f, 1f), 0.8f, 0.22f),
            Spark(new Color(0.6f, 0.85f, 1f), 1.2f, 16, 0.1f, 0.28f),
            Orbit(new Color(0.6f, 0.85f, 1f), 0.75f, 13f, 0.15f),
        });

        // ♣ 타겟형(임팩트 전용 — 지속 장판 표시는 능력 코드의 SpawnField가 담당)
        map["SlowFieldAbility"] = MakePrefab("SlowFieldVFX", new[] {
            Flash(new Color(0.5f, 1f, 0.5f), 1.6f, 0.3f),
            Burst(new Color(0.5f, 1f, 0.5f), 2f, 22, 0.16f, 0.45f),
        });
        map["FreezeAbility"] = MakePrefab("FreezeVFX", new[] {
            Flash(new Color(0.55f, 0.85f, 1f), 1.6f, 0.3f),
            Spark(new Color(0.55f, 0.85f, 1f), 2f, 26, 0.12f, 0.4f), // 얼음 파편
        });

        int assigned = AssignToAbilities(map);
        PatchVfxTimings();

        AssetDatabase.SaveAssets();
        Debug.Log("[SpellVfxPrefabBuilder] prefabs=" + map.Count + " assigned=" + assigned);
    }

    // ---------- Layer 헬퍼 ----------
    static SpellVfxComposite.Layer L(SpellVfxComposite.LayerMode m, Color c, float radius, float count, float size, float lifetime, float delay = 0f)
    {
        return new SpellVfxComposite.Layer { mode = m, color = c, radius = radius, count = count, size = size, lifetime = lifetime, delay = delay };
    }
    static SpellVfxComposite.Layer Flash(Color c, float radius, float lifetime)
        => L(SpellVfxComposite.LayerMode.Flash, c, radius, 1, 0f, lifetime);
    static SpellVfxComposite.Layer Burst(Color c, float radius, int count, float size, float lifetime)
        => L(SpellVfxComposite.LayerMode.Burst, c, radius, count, size, lifetime);
    static SpellVfxComposite.Layer Spark(Color c, float radius, int count, float size, float lifetime)
        => L(SpellVfxComposite.LayerMode.Spark, c, radius, count, size, lifetime);
    static SpellVfxComposite.Layer Implode(Color c, float radius, int count, float size, float lifetime)
        => L(SpellVfxComposite.LayerMode.Implode, c, radius, count, size, lifetime);
    static SpellVfxComposite.Layer Rise(Color c, float radius, float rate, float size)
        => L(SpellVfxComposite.LayerMode.Rise, c, radius, rate, size, 0.7f);
    static SpellVfxComposite.Layer Orbit(Color c, float radius, float rate, float size)
        => L(SpellVfxComposite.LayerMode.Orbit, c, radius, rate, size, 0.8f);
    static SpellVfxComposite.Layer Field(Color c, float radius, float rate, float size)
        => L(SpellVfxComposite.LayerMode.Field, c, radius, rate, size, 0.9f);

    // ---------- 생성/연결 ----------
    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Spell")) AssetDatabase.CreateFolder("Assets/Prefabs", "Spell");
        if (!AssetDatabase.IsValidFolder(PrefabDir)) AssetDatabase.CreateFolder("Assets/Prefabs/Spell", "Marble");
    }

    static GameObject MakePrefab(string name, SpellVfxComposite.Layer[] layers)
    {
        string path = PrefabDir + "/" + name + ".prefab";
        GameObject go = new GameObject(name);
        go.AddComponent<SpellVfxComposite>().layers = layers;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path); // 기존 경로 덮어쓰기 → GUID 유지
        Object.DestroyImmediate(go);
        return prefab;
    }

    static int AssignToAbilities(Dictionary<string, GameObject> map)
    {
        int assigned = 0;
        string[] guids = AssetDatabase.FindAssets("t:SpellAbility", new[] { "Assets/SpellMarbles/Abilities" });
        foreach (string guid in guids)
        {
            var ability = AssetDatabase.LoadAssetAtPath<SpellAbility>(AssetDatabase.GUIDToAssetPath(guid));
            if (ability == null) continue;
            GameObject prefab;
            if (!map.TryGetValue(ability.GetType().Name, out prefab)) continue; // Grenade 등 미대상은 유지
            ability.effectPrefab = prefab;
            EditorUtility.SetDirty(ability);
            assigned++;
        }
        return assigned;
    }

    // 임팩트가 잘리지 않도록 짧은 vfx 수명 상향(Heal/Overheal/Resurrection/SlowField/Freeze)
    static void PatchVfxTimings()
    {
        string[] guids = AssetDatabase.FindAssets("t:SpellAbility", new[] { "Assets/SpellMarbles/Abilities" });
        foreach (string guid in guids)
        {
            var a = AssetDatabase.LoadAssetAtPath<SpellAbility>(AssetDatabase.GUIDToAssetPath(guid));
            if (a is HealAbility h) { h.vfxDuration = Mathf.Max(h.vfxDuration, 1.2f); EditorUtility.SetDirty(h); }
            else if (a is OverhealAbility o) { o.vfxDuration = Mathf.Max(o.vfxDuration, 1.2f); EditorUtility.SetDirty(o); }
            else if (a is ResurrectionAbility r) { r.vfxDuration = Mathf.Max(r.vfxDuration, 1.2f); EditorUtility.SetDirty(r); }
            else if (a is SlowFieldAbility s) { s.vfxLifetime = Mathf.Max(s.vfxLifetime, 1f); EditorUtility.SetDirty(s); }
            else if (a is FreezeAbility f) { f.vfxLifetime = Mathf.Max(f.vfxLifetime, 1f); EditorUtility.SetDirty(f); }
        }
    }
}
