using UnityEngine;

public interface FunctionNode
{
    Vector2 position {get; set;}
}

public class WaterNode : FunctionNode
{
    Vector2 positionBase;
    public Vector2 position {get; set;}
    public float velocity;
    public float acceleration;
    public float disturbance;
    public float maxDepth;

    // const float massPerNode = 0.04f;

    #region Properties
        public float Displacement {
            get => position.y - positionBase.y;
        }
    #endregion

    #region Public Functions
        
        #region Constructors
            public WaterNode(Vector2 position, float maxDepth)
            {
                positionBase = position;
                this.position = position;
                this.maxDepth = positionBase.y - maxDepth;
            }
            public WaterNode(Vector2 position, float maxDepth, float disturbance)
            {
                positionBase = position;
                position.y += disturbance;
                this.position = position;
                this.maxDepth = positionBase.y - maxDepth;
            }
        #endregion

        public void Update(float springConstant, float damping, float massPerNode) 
        {
            Vector2 position = this.position;
            float force = springConstant * Displacement + velocity * damping;   
            acceleration = -force / massPerNode + disturbance * Time.fixedDeltaTime;
            disturbance += -disturbance * damping;

            position.y += velocity * Time.fixedDeltaTime;
            position.y = Mathf.Max(position.y, maxDepth);
            this.position = position;
            velocity += acceleration;
        }
        public float Splash(float splasherMass, float splasherVelocity, float massPerNode) 
        {
            splasherVelocity = Mathf.Min(0f, splasherVelocity);

            this.velocity = 
                (2*splasherMass*splasherVelocity + (massPerNode - splasherMass)*this.velocity) /
                (splasherMass + massPerNode)
            ;

            // this.velocity += (splasherMass / massPerNode) * .3f * splasherVelocity;
            return 
                ((splasherMass - massPerNode)*splasherVelocity + 2*massPerNode*this.velocity) /
                (splasherMass + massPerNode)
            ;
        }
        public void SplashPrime(float staticVelocity, float splasherMass, float massPerNode)
        {
            this.velocity = 
                (massPerNode - splasherMass) * this.velocity /
                (splasherMass + massPerNode)
            ;
        }
        public void Disturb(float positionDelta){
            Vector2 position = this.position;
            position.y = positionBase.y + positionDelta;
            this.position = position;
        }

        public void Reset() 
        {
            position = positionBase;
            velocity = 0;
            acceleration = 0;
        }
    #endregion
}