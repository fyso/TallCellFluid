using UnityEngine;

public class Move : MonoBehaviour
{
    [Range(0.01f, 10.0f)]
    public float m_ControllerForceFactor = 1.0f;
    Rigidbody m_Rigidbody;
    RigidBodyDataManager m_RigidBodyDataManager;
    int m_RigidBodyID;
    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_RigidBodyDataManager = GameObject.FindGameObjectsWithTag("Simulator")[0].GetComponent<RigidBodyDataManager>();

        RigidbodyInfo rigidbodyInfo = new RigidbodyInfo();
        rigidbodyInfo.m_WorldToObject = transform.worldToLocalMatrix;
        rigidbodyInfo.m_Min = GetComponent<SDFr.SDFBaker>().sdfData.bounds.min;
        rigidbodyInfo.m_BoundSize = GetComponent<SDFr.SDFBaker>().sdfData.bounds.extents * 2;
        rigidbodyInfo.m_Pos = m_Rigidbody.position;
        rigidbodyInfo.m_Velocity = m_Rigidbody.velocity;
        rigidbodyInfo.m_AngularVelocity = m_Rigidbody.angularVelocity;
        m_RigidBodyID = m_RigidBodyDataManager.RegisterRigidBody(GetComponent<SDFr.SDFBaker>().sdfData.sdfTexture, rigidbodyInfo);
    }

    private Vector3 Vector3Division(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            m_Rigidbody.useGravity = !m_Rigidbody.useGravity;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            m_Rigidbody.velocity = (new Vector3(1.0f, 0.0f, 0.0f) * m_ControllerForceFactor);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            m_Rigidbody.velocity = (new Vector3(-1.0f, 0.0f, 0.0f) * m_ControllerForceFactor);
        }
        else if (Input.GetKey(KeyCode.A))
        {
            m_Rigidbody.velocity = (new Vector3(0.0f, 0.0f, 1.0f) * m_ControllerForceFactor);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            m_Rigidbody.velocity = (new Vector3(0.0f, 0.0f, -1.0f) * m_ControllerForceFactor);
        }
        
        RigidbodyInfo rigidbodyInfo = new RigidbodyInfo();
        rigidbodyInfo.m_WorldToObject = transform.worldToLocalMatrix;
        rigidbodyInfo.m_Min = Vector3Division(GetComponent<SDFr.SDFBaker>().sdfData.bounds.min, transform.localScale);
        rigidbodyInfo.m_BoundSize = Vector3Division(GetComponent<SDFr.SDFBaker>().sdfData.bounds.extents * 2, transform.localScale);
        rigidbodyInfo.m_Pos = m_Rigidbody.position;
        rigidbodyInfo.m_Velocity = m_Rigidbody.velocity;
        rigidbodyInfo.m_AngularVelocity = m_Rigidbody.angularVelocity;
        m_RigidBodyDataManager.UpdateRigidBodyInfo(m_RigidBodyID, rigidbodyInfo);
    }
}
