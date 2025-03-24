using System;
using UnityEngine;

namespace MHLab.StickyNotes
{
    [ExecuteInEditMode]
    public class StickyNote : MonoBehaviour
    {
        public Guid Id;

        public string Title;
        [Multiline(3)]
        public string Description;
        public string Author;

        public string Tag;

        public Transform Target;

        protected bool DrawGizmo = true;

        private bool _isFirstTime = true;

        protected virtual void Start()
        {
            Initialize();
            StickyNotesManager.RegisterNote(this);
        }

        protected virtual void Update()
        {
            StickyNotesManager.RegisterNote(this);
        }

        protected virtual void OnValidate()
        {
            Initialize();
        }

        protected void OnDrawGizmos()
        {
            if(DrawGizmo)
                Gizmos.DrawIcon(this.transform.position, "../MHLab/StickyNotes/Gizmos/stickynotes_gizmo_icon.png", true);
        }

        protected virtual void OnDestroy()
        {
            StickyNotesManager.UnregisterNote(this);
        }

        public virtual void Initialize()
        {
            if (_isFirstTime)
            {
                _isFirstTime = false;
                Id = Guid.NewGuid();

                StickyNotesManager.Initialize();
            }
        }
    }
}
