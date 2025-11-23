using UnityEngine;
using HInteractions;

namespace HGame.Objects
{
    public class HoseNozzle : Liftable
    {
        [Header("Hose Settings")]
        [SerializeField] private GameObject waterParticleVFX; // Assign partikel air di sini
        [SerializeField] private float shootRecoilForce = 5f;

        private bool _isShooting = false;

        protected override void Awake()
        {
            base.Awake();
            maxHolders = 1; // Selang cuma bisa dipegang 1 orang
            if (waterParticleVFX) waterParticleVFX.SetActive(false);
        }

        protected override void OnFirstPickup()
        {
            // Selang melayang (Gravity OFF) biar mudah diarahkan
            Rigidbody.useGravity = false; 
            Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            
            // Massa ringan tapi jangan 0
            Rigidbody.mass = 5f; 
            Rigidbody.drag = 3f; // Drag tinggi udara biar ga liar
            Rigidbody.angularDrag = 3f;
        }

        protected override void OnAllDropped()
        {
            Rigidbody.useGravity = true;
            Rigidbody.interpolation = RigidbodyInterpolation.None;
            SetShooting(false); // Matikan air kalau jatuh
        }

        // Fungsi yang bisa dipanggil PlayerController lewat Input
        public void ToggleShooting()
        {
            if (!IsLifted) return;
            SetShooting(!_isShooting);
        }

        public void SetShooting(bool state)
        {
            _isShooting = state;
            if (waterParticleVFX) waterParticleVFX.SetActive(state);
        }

        private void FixedUpdate()
        {
            // Efek dorongan ke belakang saat nembak air
            if (_isShooting && IsLifted)
            {
                Rigidbody.AddForce(-transform.forward * shootRecoilForce, ForceMode.Force);
            }
        }
    }
}