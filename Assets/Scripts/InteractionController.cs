using UnityEngine;
using NaughtyAttributes;
using HInteractions;
using System;

namespace HPlayer
{
    public class InteractionController : MonoBehaviour, IObjectHolder
    {
        [Header("Hold Settings")]
        [SerializeField, Required] private Transform handTransform;
        [SerializeField, Required] private Collider handTrigger;
        [SerializeField, Min(0.1f)] private float holdingForce = 8f; // lebih tinggi biar responsif
        [SerializeField] private int heldObjectLayer;
        [SerializeField, Range(0f, 90f)] private float heldClampXRotation = 45f;

        [Header("Offset Settings")]
        [SerializeField] private Vector3 holdPositionOffset = Vector3.zero;
        [SerializeField] private Vector3 holdRotationOffset = Vector3.zero;

        [field: SerializeField, ReadOnly] public Liftable HeldObject { get; private set; } = null;

        [field: Header("Input")]
        [field: SerializeField, ReadOnly] public bool Interacting { get; private set; } = false;

        public event Action OnInteractionStart;
        public event Action OnInteractionEnd;

        private Liftable currentCandidate;

        // Implementasi interface
        public Interactable SelectedObject => currentCandidate != null ? currentCandidate : HeldObject;

        private void OnEnable()
        {
            OnInteractionStart += ChangeHeldObject;
            PlayerController.OnPlayerEnterPortal += CheckHeldObjectOnTeleport;
        }

        private void OnDisable()
        {
            OnInteractionStart -= ChangeHeldObject;
            PlayerController.OnPlayerEnterPortal -= CheckHeldObjectOnTeleport;
        }

        private void Update()
        {
            UpdateInput();
        }

        private void FixedUpdate()
        {
            if (HeldObject)
                UpdateHeldObjectPosition();
        }

        #region - Input -

        private void UpdateInput()
        {
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
            if (HeldObject) return; // sudah memegang sesuatu

            if (other.TryGetComponent(out Liftable liftable))
                currentCandidate = liftable;
        }

        private void OnTriggerExit(Collider other)
        {
            if (currentCandidate && other.TryGetComponent(out Liftable liftable) && liftable == currentCandidate)
                currentCandidate = null;
        }

        #endregion

        #region - Hold System -

        private void UpdateHeldObjectPosition()
        {
            if (HeldObject == null) return;

            Rigidbody rb = HeldObject.Rigidbody;
            if (!rb) return;

            // Hitung posisi & rotasi target
            Vector3 targetPos = handTransform.TransformPoint(holdPositionOffset);
            Quaternion targetRot = handTransform.rotation * Quaternion.Euler(holdRotationOffset + HeldObject.LiftDirectionOffset);

            // Gunakan MovePosition dan MoveRotation agar responsif & stabil
            rb.MovePosition(Vector3.Lerp(rb.position, targetPos, Time.fixedDeltaTime * holdingForce));
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.fixedDeltaTime * holdingForce));
        }

        private void ChangeHeldObject()
        {
            if (HeldObject)
                DropObject(HeldObject);
            else if (currentCandidate)
                PickUpObject(currentCandidate);
        }

        private void PickUpObject(Liftable obj)
        {
            if (obj == null)
            {
                Debug.LogWarning($"{nameof(InteractionController)}: Attempted to pick up null object!");
                return;
            }

            HeldObject = obj;
            currentCandidate = null;
            obj.PickUp(this, heldObjectLayer);

            // Langsung snap ke tangan di frame pertama
            Rigidbody rb = HeldObject.Rigidbody;
            if (rb)
            {
                rb.position = handTransform.TransformPoint(holdPositionOffset);
                rb.rotation = handTransform.rotation * Quaternion.Euler(holdRotationOffset + HeldObject.LiftDirectionOffset);
            }
        }

        private void DropObject(Liftable obj)
        {
            if (obj == null)
            {
                Debug.LogWarning($"{nameof(InteractionController)}: Attempted to drop null object!");
                return;
            }

            HeldObject = null;
            obj.Drop();
        }

        private void CheckHeldObjectOnTeleport()
        {
            if (HeldObject != null)
                DropObject(HeldObject);
        }

        #endregion
    }
}
