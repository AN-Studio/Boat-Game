using UnityEngine;

public partial class WaterGenerator
{
    public class WaterNode
    {
        Vector2 positionBase;
        public Vector2 position;
        public float velocity;
        public float acceleration;
        public float disturbance;

        // const float massPerNode = 0.04f;

        #region Properties
            public float Displacement {
                get => position.y - positionBase.y;
            }
        #endregion

        #region Public Functions
            
            #region Construnctors
                public WaterNode(Vector2 position)
                {
                    positionBase = position;
                    this.position = position;
                }
                public WaterNode(Vector2 position, float disturbance)
                {
                    positionBase = position;
                    this.position = positionBase;
                    this.position.y += disturbance;
                }
            #endregion

            public void Update(float springConstant, float damping, float massPerNode) 
            {
                float force = springConstant * Displacement + velocity * damping;   
                acceleration = -force / massPerNode + disturbance * Time.fixedDeltaTime;
                disturbance += -disturbance * damping;

                position.y += velocity * Time.fixedDeltaTime;
                velocity += acceleration;
            }
            public void Splash(float momentum, float massPerNode) {
                momentum = Mathf.Min(0f, momentum);
                this.velocity += momentum / massPerNode * Time.fixedDeltaTime;
            }
            public void Disturb(float positionDelta){
                this.position.y = positionBase.y + positionDelta;
            }
        #endregion
    }

}