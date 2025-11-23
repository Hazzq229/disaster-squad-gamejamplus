using UnityEngine;
using HInteractions;

namespace HGame.Objects
{
    public class TwoPlayerLiftable : Liftable
    {
        [Header("Heavy Settings")]
        [SerializeField] private float heavyMass = 60f;
        [SerializeField] private float coopMassDivisor = 4f;

        [Header("Heavy Penalties (Tuning)")]
        [SerializeField, Range(0f, 1f)] private float penaltySolo = 0.6f; 
        [SerializeField, Range(0f, 1f)] private float penaltyCoop = 0.1f;
        public override float SpeedPenalty
        {
            get
            {
                if(Holders.Count >=2) return penaltyCoop;
                return penaltySolo;
            }
        }
        public override bool ForceFaceObject
        {
            get
            {
                return true;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            maxHolders = 2;
            Rigidbody.mass = heavyMass;
        }

        public override Vector3 GetGrabPoint(Vector3 handPosition)
        {
            Collider col = GetComponentInChildren<Collider>();
            if(col != null)
                return col.ClosestPoint(handPosition);
            return base.GetGrabPoint(handPosition);
        }

        protected override void OnFirstPickup()
        {
            Rigidbody.useGravity = true;
            Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            
            Rigidbody.drag = 0.5f; 
            Rigidbody.angularDrag = 0.5f;

            UpdatePhysicsState();
        }

        protected override void OnAllDropped()
        {
            base.OnAllDropped();
            Rigidbody.mass = heavyMass;
        }

        protected override void OnAnyPickup() => UpdatePhysicsState();
        protected override void OnAnyDrop() => UpdatePhysicsState();

        private void UpdatePhysicsState()
        {
            if (Holders.Count > 1)
                Rigidbody.mass = heavyMass / coopMassDivisor;
            else
                Rigidbody.mass = heavyMass;
        }
    }
}