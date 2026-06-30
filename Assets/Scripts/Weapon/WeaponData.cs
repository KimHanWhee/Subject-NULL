using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapon/WeaponData")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public GameObject bulletPrefab; // 무기마다 다른 총알을 여기서 갈아끼움
    public AudioClip shotSound;
    public float damage;
    public float fireRate;
    public float bulletSpeed; // 총알 속도도 무기별로 다르면 여기 추가
}
