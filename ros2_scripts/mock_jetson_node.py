import rclpy
from rclpy.node import Node
from std_msgs.msg import Float32, Bool
from geometry_msgs.msg import Twist
import math

class MockJetsonNode(Node):
    def __init__(self):
        super().__init__('mock_joystick_node')
        
        # Publishers (Send data to Unity)
        self.pub_vel = self.create_publisher(Twist, 'nav/cmd_vel', 10)
        self.pub_margin = self.create_publisher(Float32, 'nav/margin', 10)
        self.pub_shadow = self.create_publisher(Bool, 'nav/shadow_toggle', 10)
        
        # Subscribers (Listen for button clicks from Unity)
        self.sub_toggle = self.create_subscription(Bool, 'nav/cmd/toggle_shadow', self.toggle_callback, 10)

        self.timer = self.create_timer(0.05, self.publish_telemetry) # 20Hz
        self.count = 0
        self.shadow_on = False
        self.get_logger().info("Mock Jetson Node Started (Send data to Unity at 127.0.0.1:10000)")

    def toggle_callback(self, msg):
        self.shadow_on = not self.shadow_on
        self.get_logger().info(f"Toggling Shadow Mode via Unity: {self.shadow_on}")

    def publish_telemetry(self):
        # 1. Simulate Robot Moving (Sine wave)
        t = self.count * 0.1
        vel_msg = Twist()
        vel_msg.linear.x = 1.0 * (0.5 + 0.5 * math.sin(t)) # Oscillate speed
        self.pub_vel.publish(vel_msg)

        # 2. Simulate Safety Margin (Dropping then recovering)
        margin = 2.0
        if self.shadow_on: margin = 0.2 # Simulate near miss
        self.pub_margin.publish(Float32(data=margin))

        # 3. Send Shadow Status
        self.pub_shadow.publish(Bool(data=self.shadow_on))
        
        self.count += 1

if __name__ == '__main__':
    rclpy.init()
    node = MockJetsonNode()
    rclpy.spin(node)
    rclpy.shutdown()
