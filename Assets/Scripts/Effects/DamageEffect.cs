using UnityEngine;

public class DamageEffect : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float lifetime = 0.5f;

    private float playbackDuration;

    public static void Create(Vector3 position)
    {
        GameAssets assets = GameAssets.Instance;
        if (assets == null || assets.DamageEffectPrefab == null)
            return;

        Instantiate(assets.DamageEffectPrefab, position, Quaternion.identity, assets.EffectsParent);
    }

    private void Awake()
    {
        playbackDuration = Mathf.Max(0.01f, lifetime);

        Animator animator = GetComponent<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        animator.enabled = true;
        animator.Play(0, 0, 0f);
        animator.Update(0f);

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        if (clips.Length > 0 && clips[0] != null)
            playbackDuration = Mathf.Max(0.01f, clips[0].length);
    }

    private void Start()
    {
        Destroy(gameObject, playbackDuration);
    }
}
