using UnityEngine;
using NaughtyAttributes;
using HInteractions;
using System;

namespace HPlayer
{
    [RequireComponent(typeof(Rigidbody))] 
    public class InteractionController : MonoBehaviour, IObjectHolder
    {
        [Header("Hold Settings")]
        [SerializeField, Required] private Transform handTransform;
        [SerializeField, Required] private Collider handTrigger;
        [SerializeField] private int heldObjectLayer;
        
        [Header("Physics Joint Settings")]
        [Tooltip("Kekuatan pegas (makin tinggi makin kaku)")][SerializeField] private float jointSpring = 1500f;
        [Tooltip("Peredam getaran agar biar tidak memantul berlebihan")][SerializeField] private float jointDamper = 100f;
        [Tooltip("Kekuatan memutar objek")][SerializeField] private float rotateForce = 200f;   

        [Header("Throw Settings")]
        [SerializeField] private float throwForce = 10f;

        [field: SerializeField, ReadOnly] public Liftable HeldObject { get; private set; } = null;
        [field: Header("Input")]
        [field: SerializeField, ReadOnly] public bool Interacting { get; private set; } = false;

        public event Action OnInteractionStart;
        public event Action OnInteractionEnd;

        private Liftable currentCandidate;
        private SpringJoint grabJoint;
        [SerializeField] private Rigidbody playerRb;

        public Interactable SelectedObject => currentCandidate != null ? currentCandidate : HeldObject;

        private void Awake()
        {
            playerRb = GetComponentInParent<Rigidbody>();
        }

        private void OnEnable()
        {
            OnInteractionStart += ChangeHeldObject;
        }

        private void OnDisable()
        {
            OnInteractionStart -= ChangeHeldObject;
        }

        private void Update()
        {
            UpdateInput();
        }

        private void FixedUpdate()
        {
            if (HeldObject)
            {
                RotateHeldObjectPhysics();
            }
        }

        #region - Input -

        private void UpdateInput()
        {
            // Input System lama (bisa diganti New Input System sesuai script sebelumnya)
            bool interacting = Input.GetMouseButton(0); 
            
            if (interacting != Interacting)
            {
                Interacting = interacting;
                if (interacting)
                    OnInteractionStart?.Invoke();
                else
                    OnInteractionEnd?.Invoke();
            }
        }

        #endregion

        #region - Trigger Detection -

        private void OnTriggerEnter(Collider other)
        {
            if (HeldObject) return;

            if (other.TryGetComponent(out Liftable liftable))
            {
                currentCandidate = liftable;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (currentCandidate && other.TryGetComponent(out Liftable liftable) && liftable == currentCandidate)
                currentCandidate = null;
        }

        #endregion

        #region - Hold System (Physics Based) -

        // Fungsi ini menggantikan UpdateHeldObjectPosition yang lama
        private void RotateHeldObjectPhysics()
        {
            if (HeldObject == null || HeldObject.Rigidbody == null) return;

            Rigidbody objRb = HeldObject.Rigidbody;

            // 1. Tentukan rotasi target (sesuai arah tangan player)
            Quaternion targetRotation = handTransform.rotation * Quaternion.Euler(HeldObject.LiftDirectionOffset);

            // 2. Hitung perbedaan rotasi
            // Ini teknik merubah Quaternion diff menjadi Vector3 angular velocity
            Quaternion rotationDiff = targetRotation * Quaternion.Inverse(objRb.rotation);
            rotationDiff.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);

            // Normalisasi sudut (biar handle > 180 derajat)
            if (angleInDegrees > 180f) angleInDegrees -= 360f;

            // Kunci: Hanya putar jika sudutnya signifikan (optimasi)
            if (Mathf.Abs(angleInDegrees) > 1f) 
            {
                // Konversi derajat ke radian untuk fisika
                Vector3 angularDisplacement = rotationAxis * (angleInDegrees * Mathf.Deg2Rad);
                
                // Terapkan Torque (Gaya putar)
                // Kita kurangi dengan angularVelocity sekarang supaya gerakan tidak overshoot (Damping)
                Vector3 torque = (angularDisplacement * rotateForce) - (objRb.angularVelocity * 10f);
                
                objRb.AddTorque(torque, ForceMode.Acceleration);
            }
        }

        private void ChangeHeldObject()
        {
            if (HeldObject)
                DropObject(HeldObject, throwObject: true); // Tambah fitur lempar
            else if (currentCandidate)
                PickUpObject(currentCandidate);
        }

        private void PickUpObject(Liftable obj)
        {
            if (obj == null) return;

            HeldObject = obj;
            currentCandidate = null;
            
            // Panggil method interface Liftable (ganti layer, dll)
            obj.PickUp(this, heldObjectLayer);

            // --- SETUP PHYSICS JOINT ---
            // Kita pasang joint di PLAYER, lalu sambungkan ke OBJEK
            grabJoint = gameObject.AddComponent<SpringJoint>();
            grabJoint.autoConfigureConnectedAnchor = false;
            
            // Anchor di player (posisi tangan relative terhadap player)
            grabJoint.anchor = transform.InverseTransformPoint(handTransform.position);
            
            // Anchor di objek (pusat objek)
            grabJoint.connectedAnchor = Vector3.zero;
            
            grabJoint.connectedBody = obj.Rigidbody;

            // Setup Kekuatan Pegas
            grabJoint.spring = jointSpring;
            grabJoint.damper = jointDamper;
            
            // Biar objek gak muter liar saat ditarik
            obj.Rigidbody.angularDrag = 5f; 
            obj.Rigidbody.drag = 2f;
        }

        private void DropObject(Liftable obj, bool throwObject = false)
        {
            if (obj == null) return;

            // Hancurkan Joint (Lepas pegangan)
            if (grabJoint != null)
            {
                Destroy(grabJoint);
            }

            // Kembalikan drag normal (biar pas jatuh dia gelinding normal)
            obj.Rigidbody.angularDrag = 0.05f; 
            obj.Rigidbody.drag = 0f;

            // Fitur Lempar (Momentum)
            if (throwObject && playerRb)
            {
                // Gabungkan velocity player + arah hadap
                Vector3 throwVec = playerRb.velocity + (transform.forward * throwForce);
                obj.Rigidbody.velocity = throwVec;
            }

            HeldObject = null;
            obj.Drop();
        }

        #endregion
    }
}