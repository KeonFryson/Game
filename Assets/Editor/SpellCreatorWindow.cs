using UnityEngine;
using UnityEditor;
using System.IO;

public class SpellCreatorWindow : EditorWindow
{
    private string spellName = "New Spell";
    private int spellID = 0;
    private string spellDescription = "";
    private int maxStackSize = 1;
    private Sprite spellIcon;
    private GameObject spellPrefab;

    // Spell Properties
    private int manaCost = 10;
    private float channelTime = 0f;
    private float cooldown = 1f;
    private float spellSpeed = 10f;
    private GameObject spellEffectPrefab;
    private AnimationCurve channelScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    private Vector3 spellCastPointOffset = Vector3.zero;
    private Transform customSpellCastPoint = null;

    // Damage settings
    private float baseDamage = 10f;

    // Projectile settings
    private float projectileLifetime = 5f;
    private bool useGravity = false;
    private bool isHoming = false;
    private float homingStrength = 5f;

    // Save location
    private string savePath = "Assets/ScriptableObjects/Items/Spells";
    private bool createProjectile = true;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Spell Creator")]
    public static void ShowWindow()
    {
        SpellCreatorWindow window = GetWindow<SpellCreatorWindow>("Spell Creator");
        window.minSize = new Vector2(400, 600);
    }

    private void OnEnable()
    {
        // Auto-generate next available ID
        spellID = GetNextAvailableSpellID();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Spell Creator", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // Basic Information
        DrawSection("Basic Information", () =>
        {
            spellName = EditorGUILayout.TextField("Spell Name", spellName);
            spellID = EditorGUILayout.IntField("Spell ID", spellID);

            if (GUILayout.Button("Auto-Generate ID"))
            {
                spellID = GetNextAvailableSpellID();
            }

            EditorGUILayout.LabelField("Description");
            spellDescription = EditorGUILayout.TextArea(spellDescription, GUILayout.Height(60));
            maxStackSize = EditorGUILayout.IntSlider("Max Stack Size", maxStackSize, 1, 999);
            spellIcon = (Sprite)EditorGUILayout.ObjectField("Spell Icon", spellIcon, typeof(Sprite), false);
            spellPrefab = (GameObject)EditorGUILayout.ObjectField("Item Prefab", spellPrefab, typeof(GameObject), false);
        });

        EditorGUILayout.Space(10);

        // Spell Properties
        DrawSection("Spell Properties", () =>
        {
            manaCost = EditorGUILayout.IntField("Mana Cost", manaCost);
            channelTime = EditorGUILayout.FloatField("Channel Time (s)", channelTime);
            cooldown = EditorGUILayout.FloatField("Cooldown (s)", cooldown);
            spellSpeed = EditorGUILayout.FloatField("Spell Speed", spellSpeed);
            spellEffectPrefab = (GameObject)EditorGUILayout.ObjectField("Spell Effect Prefab", spellEffectPrefab, typeof(GameObject), false);
            channelScaleCurve = EditorGUILayout.CurveField("Channel Scale Curve", channelScaleCurve);
            spellCastPointOffset = EditorGUILayout.Vector3Field("Cast Point Offset", spellCastPointOffset);
            customSpellCastPoint = (Transform)EditorGUILayout.ObjectField("Custom Cast Point", customSpellCastPoint, typeof(Transform), true);
        });

        EditorGUILayout.Space(10);

        // Projectile Settings
        createProjectile = EditorGUILayout.Toggle("Create Projectile Prefab", createProjectile);

        if (createProjectile)
        {
            DrawSection("Projectile Settings", () =>
            {
                baseDamage = EditorGUILayout.FloatField("Base Damage", baseDamage);
                projectileLifetime = EditorGUILayout.FloatField("Lifetime (s)", projectileLifetime);
                useGravity = EditorGUILayout.Toggle("Use Gravity", useGravity);
                isHoming = EditorGUILayout.Toggle("Is Homing", isHoming);

                if (isHoming)
                {
                    homingStrength = EditorGUILayout.FloatField("Homing Strength", homingStrength);
                }
            });
        }

        EditorGUILayout.Space(10);

        // Save Settings
        DrawSection("Save Settings", () =>
        {
            EditorGUILayout.LabelField("Save Path", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            savePath = EditorGUILayout.TextField(savePath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Save Location", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    savePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();
        });

        EditorGUILayout.Space(20);

        // Create Button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Create Spell", GUILayout.Height(40)))
        {
            CreateSpell();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        // Quick Templates
        if (GUILayout.Button("Load Template: Fireball"))
        {
            LoadFireballTemplate();
        }
        if (GUILayout.Button("Load Template: Ice Shard"))
        {
            LoadIceShardTemplate();
        }
        if (GUILayout.Button("Load Template: Lightning Bolt"))
        {
            LoadLightningBoltTemplate();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSection(string title, System.Action content)
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        content();
        EditorGUILayout.EndVertical();
    }

    private void CreateSpell()
    {
        if (string.IsNullOrEmpty(spellName))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a spell name!", "OK");
            return;
        }

        // Ensure directory exists
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        // Create the spell effect prefab if needed
        GameObject effectPrefab = spellEffectPrefab;
        if (createProjectile && effectPrefab == null)
        {
            effectPrefab = CreateProjectilePrefab();
        }

        // Create the ItemData asset
        ItemData spellData = ScriptableObject.CreateInstance<ItemData>();

        // Set basic properties
        spellData.itemName = spellName;
        spellData.itemID = spellID;
        spellData.itemDescription = spellDescription;
        spellData.maxStackSize = maxStackSize;
        spellData.itemIcon = spellIcon;
        spellData.itemPrefab = spellPrefab;

        // Set spell properties
        spellData.isSpell = true;
        spellData.manaCost = manaCost;
        spellData.spellChannelTime = channelTime;
        spellData.spellCooldown = cooldown;
        spellData.spellSpeed = spellSpeed;
        spellData.spellEffectPrefab = effectPrefab;
        spellData.channelScaleCurve = channelScaleCurve;
        spellData.spellCastPointOffset = spellCastPointOffset;
        spellData.customSpellCastPoint = customSpellCastPoint;

        // Disable other types
        spellData.isWeapon = false;
        spellData.isConsumable = false;

        // Save the asset
        string assetPath = $"{savePath}/{spellName}.asset";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        AssetDatabase.CreateAsset(spellData, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = spellData;

        EditorUtility.DisplayDialog("Success", $"Spell '{spellName}' created successfully at:\n{assetPath}", "OK");

        // Auto-increment ID for next spell
        spellID++;
    }

    private GameObject CreateProjectilePrefab()
    {
        // Create prefab directory
        string prefabPath = "Assets/Prefabs/Spells";
        if (!Directory.Exists(prefabPath))
        {
            Directory.CreateDirectory(prefabPath);
        }

        // Create a new GameObject for the projectile
        GameObject projectile = new GameObject($"{spellName}_Projectile");

        // Add sphere mesh for visualization
        GameObject visualMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visualMesh.transform.SetParent(projectile.transform);
        visualMesh.transform.localPosition = Vector3.zero;
        visualMesh.transform.localScale = Vector3.one * 0.5f;
        visualMesh.name = "Visual";

        // Remove the collider from the visual (we'll add one to the parent)
        DestroyImmediate(visualMesh.GetComponent<Collider>());

        // Add components to parent
        SphereCollider collider = projectile.AddComponent<SphereCollider>();
        collider.radius = 0.3f;
        collider.isTrigger = false;

        Rigidbody rb = projectile.AddComponent<Rigidbody>();
        rb.useGravity = useGravity;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        SpellProjectile spellProjectile = projectile.AddComponent<SpellProjectile>();

        // Use reflection to set private fields (since they're serialized)
        SerializedObject so = new SerializedObject(spellProjectile);
        so.FindProperty("speed").floatValue = spellSpeed;
        so.FindProperty("lifetime").floatValue = projectileLifetime;
        so.FindProperty("useGravity").boolValue = useGravity;
        so.FindProperty("baseDamage").floatValue = baseDamage;
        so.FindProperty("isHoming").boolValue = isHoming;
        so.FindProperty("homingStrength").floatValue = homingStrength;
        so.ApplyModifiedProperties();

        // Add a particle system for visual effect
        ParticleSystem ps = projectile.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 0f;
        main.startSize = 0.3f;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = 50f;

        // Save as prefab
        string prefabAssetPath = $"{prefabPath}/{spellName}_Projectile.prefab";
        prefabAssetPath = AssetDatabase.GenerateUniqueAssetPath(prefabAssetPath);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(projectile, prefabAssetPath);
        DestroyImmediate(projectile);

        return savedPrefab;
    }

    private int GetNextAvailableSpellID()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        int maxID = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (item != null && item.isSpell && item.itemID > maxID)
            {
                maxID = item.itemID;
            }
        }

        return maxID + 1;
    }

    private void LoadFireballTemplate()
    {
        spellName = "Fireball";
        spellDescription = "A blazing ball of fire that deals damage on impact";
        manaCost = 15;
        channelTime = 1.5f;
        cooldown = 2f;
        spellSpeed = 12f;
        baseDamage = 25f;
        projectileLifetime = 5f;
        useGravity = false;
        isHoming = false;
        createProjectile = true;
        spellCastPointOffset = new Vector3(0, 1, 0.5f);
    }

    private void LoadIceShardTemplate()
    {
        spellName = "Ice Shard";
        spellDescription = "A sharp shard of ice that pierces enemies";
        manaCost = 10;
        channelTime = 0.5f;
        cooldown = 1f;
        spellSpeed = 20f;
        baseDamage = 15f;
        projectileLifetime = 4f;
        useGravity = false;
        isHoming = false;
        createProjectile = true;
        spellCastPointOffset = new Vector3(0, 1, 0.5f);
    }

    private void LoadLightningBoltTemplate()
    {
        spellName = "Lightning Bolt";
        spellDescription = "A homing bolt of lightning that seeks its target";
        manaCost = 20;
        channelTime = 1f;
        cooldown = 3f;
        spellSpeed = 15f;
        baseDamage = 30f;
        projectileLifetime = 6f;
        useGravity = false;
        isHoming = true;
        homingStrength = 8f;
        createProjectile = true;
        spellCastPointOffset = new Vector3(0, 1.5f, 0.5f);
    }
}