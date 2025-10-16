using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FitzHughNagumo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private List<Point3D> trajectoryPoints;
        private GeometryModel3D trajectoryModel;
        private MeshGeometry3D trajectoryMesh;
        private PerspectiveCamera camera;

        // Camera control
        private Point lastMousePos;
        private bool isRotating = false;
        private double cameraTheta = 45;
        private double cameraPhi = 30;
        private double cameraDistance = 8;

        // FitzHugh-Nagumo parameters
        private double v = 0.1;
        private double w = 0.0;
        private double t = 0.0;
        private double dt = 0.02; // Faster time step

        private double epsilon = 0.08;
        private double a = 0.7;
        private double b = 0.8;
        private double I = 0.5;

        private int maxPoints = 8000;
        private int stepsPerFrame = 3; // Multiple steps per frame for faster animation

        public MainWindow()
        {
            Title = "FitzHugh-Nagumo 3D - Drag to Rotate, Scroll to Zoom";
            Width = 1200;
            Height = 800;
            InitializeComponent();
            InitializeScene();
            InitializeTimer();
        }

        private void InitializeScene()
        {
            trajectoryPoints = new List<Point3D>();

            var viewport = new Viewport3D();

            // Camera
            camera = new PerspectiveCamera
            {
                FieldOfView = 60,
                NearPlaneDistance = 0.1,
                FarPlaneDistance = 100
            };
            UpdateCameraPosition();
            viewport.Camera = camera;

            // Mouse controls
            viewport.MouseLeftButtonDown += Viewport_MouseLeftButtonDown;
            viewport.MouseLeftButtonUp += Viewport_MouseLeftButtonUp;
            viewport.MouseMove += Viewport_MouseMove;
            viewport.MouseWheel += Viewport_MouseWheel;

            // Lights
            var lightGroup = new Model3DGroup();
            lightGroup.Children.Add(new AmbientLight(Colors.Gray));
            lightGroup.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, -1, -2)));
            lightGroup.Children.Add(new DirectionalLight(Colors.White, new Vector3D(1, -0.5, -1)));

            var lightModel = new ModelVisual3D { Content = lightGroup };
            viewport.Children.Add(lightModel);

            // Trajectory
            trajectoryMesh = new MeshGeometry3D();

            var gradientBrush = new LinearGradientBrush();
            gradientBrush.GradientStops.Add(new GradientStop(Colors.Blue, 0));
            gradientBrush.GradientStops.Add(new GradientStop(Colors.Cyan, 0.5));
            gradientBrush.GradientStops.Add(new GradientStop(Colors.Yellow, 1));

            var material = new DiffuseMaterial(gradientBrush);
            material.Brush.Opacity = 0.9;
            trajectoryModel = new GeometryModel3D(trajectoryMesh, material);

            var trajectoryVisual = new ModelVisual3D { Content = trajectoryModel };
            viewport.Children.Add(trajectoryVisual);

            // Add coordinate system
            AddCoordinateSystem(viewport);

            // Create UI
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Viewport with legend overlay
            var viewportContainer = new Grid();
            viewportContainer.Children.Add(viewport);
            viewportContainer.Children.Add(CreateLegend());

            Grid.SetRow(viewportContainer, 0);
            grid.Children.Add(viewportContainer);

            var controlPanel = CreateControlPanel();
            Grid.SetRow(controlPanel, 1);
            grid.Children.Add(controlPanel);

            Content = grid;
        }

        private void UpdateCameraPosition()
        {
            double x = cameraDistance * Math.Sin(cameraPhi * Math.PI / 180) * Math.Cos(cameraTheta * Math.PI / 180);
            double z = cameraDistance * Math.Sin(cameraPhi * Math.PI / 180) * Math.Sin(cameraTheta * Math.PI / 180);
            double y = cameraDistance * Math.Cos(cameraPhi * Math.PI / 180);

            camera.Position = new Point3D(x, y, z);
            camera.LookDirection = new Vector3D(-x, -y, -z);
            camera.UpDirection = new Vector3D(0, 1, 0);
        }

        private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isRotating = true;
            lastMousePos = e.GetPosition((IInputElement)sender);
            ((UIElement)sender).CaptureMouse();
        }

        private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isRotating = false;
            ((UIElement)sender).ReleaseMouseCapture();
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isRotating) return;

            var currentPos = e.GetPosition((IInputElement)sender);
            var delta = currentPos - lastMousePos;

            cameraTheta += delta.X * 0.5;
            cameraPhi = Math.Max(5, Math.Min(175, cameraPhi - delta.Y * 0.5));

            UpdateCameraPosition();
            lastMousePos = currentPos;
        }

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            cameraDistance *= e.Delta > 0 ? 0.9 : 1.1;
            cameraDistance = Math.Max(2, Math.Min(20, cameraDistance));
            UpdateCameraPosition();
        }

        private StackPanel CreateControlPanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = new SolidColorBrush(Color.FromArgb(220, 20, 20, 30)),
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            AddSlider(panel, "Current (I):", 0, 2.0, I, (v) => { I = v; });
            AddSlider(panel, "ε:", 0.01, 0.3, epsilon, (v) => { epsilon = v; });
            AddSlider(panel, "a:", 0.1, 1.5, a, (v) => { a = v; });

            var resetButton = new Button
            {
                Content = "Reset",
                Margin = new Thickness(20, 5, 5, 5),
                Padding = new Thickness(15, 8, 15, 8),
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Color.FromArgb(255, 70, 130, 180))
            };
            resetButton.Click += (s, e) => ResetSimulation();
            panel.Children.Add(resetButton);

            return panel;
        }

        private StackPanel CreateLegend()
        {
            var legend = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Background = new SolidColorBrush(Color.FromArgb(200, 20, 20, 30)),               
                Margin = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            var title = new TextBlock
            {
                Text = "Axes Legend",
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            legend.Children.Add(title);

            AddLegendItem(legend, Colors.Red, "V-axis: Membrane Potential");
            AddLegendItem(legend, Colors.Lime, "W-axis: Recovery Variable");
            AddLegendItem(legend, Colors.DodgerBlue, "T-axis: Time");

            var separator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                Margin = new Thickness(0, 8, 0, 8)
            };
            legend.Children.Add(separator);

            var instructions = new TextBlock
            {
                Text = "🖱️ Drag to rotate\n🔍 Scroll to zoom",
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                FontSize = 11,
                LineHeight = 18
            };
            legend.Children.Add(instructions);

            return legend;
        }

        private void AddLegendItem(StackPanel parent, Color color, string text)
        {
            var item = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 3, 0, 3)
            };

            var colorBox = new Border
            {
                Width = 16,
                Height = 16,
                Background = new SolidColorBrush(color),
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var label = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };

            item.Children.Add(colorBox);
            item.Children.Add(label);
            parent.Children.Add(item);
        }

        private void AddSlider(StackPanel panel, string label, double min, double max, double initial, Action<double> onChange)
        {
            panel.Children.Add(new TextBlock
            {
                Text = label,
                Foreground = Brushes.White,
                Margin = new Thickness(15, 5, 5, 5),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold
            });

            var slider = new Slider
            {
                Width = 120,
                Minimum = min,
                Maximum = max,
                Value = initial,
                VerticalAlignment = VerticalAlignment.Center
            };

            var valueLabel = new TextBlock
            {
                Text = initial.ToString("F2"),
                Foreground = Brushes.White,
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 35,
                FontWeight = FontWeights.Bold
            };

            slider.ValueChanged += (s, e) =>
            {
                onChange(e.NewValue);
                valueLabel.Text = e.NewValue.ToString("F2");
            };

            panel.Children.Add(slider);
            panel.Children.Add(valueLabel);
        }

        private void AddCoordinateSystem(Viewport3D viewport)
        {
            AddAxisArrow(viewport, new Point3D(0, 0, 0), new Vector3D(2.5, 0, 0), Colors.Red, "V");
            AddAxisArrow(viewport, new Point3D(0, 0, 0), new Vector3D(0, 2.5, 0), Colors.Lime, "W");
            AddAxisArrow(viewport, new Point3D(0, 0, 0), new Vector3D(0, 0, 2.5), Colors.DodgerBlue, "T");

            // Grid planes
            AddGridPlane(viewport, Colors.Gray, 0.1);
        }

        private void AddAxisArrow(Viewport3D viewport, Point3D start, Vector3D direction, Color color, string label)
        {
            var mesh = new MeshGeometry3D();
            var end = start + direction;

            // Axis line as cylinder
            AddCylinder(mesh, start, end, 0.03);

            // Arrow head as cone
            var coneStart = end;
            var coneEnd = end + 0.2 * direction / direction.Length;
            AddCone(mesh, coneStart, coneEnd, 0.08);

            var material = new DiffuseMaterial(new SolidColorBrush(color));
            var model = new GeometryModel3D(mesh, material);
            viewport.Children.Add(new ModelVisual3D { Content = model });
        }

        private void AddCylinder(MeshGeometry3D mesh, Point3D p1, Point3D p2, double radius)
        {
            int baseIndex = mesh.Positions.Count;
            Vector3D axis = p2 - p1;
            axis.Normalize();

            Vector3D perp = Math.Abs(axis.Y) > 0.9 ? new Vector3D(1, 0, 0) : new Vector3D(0, 1, 0);
            Vector3D u = Vector3D.CrossProduct(axis, perp);
            u.Normalize();
            Vector3D v = Vector3D.CrossProduct(axis, u);

            int segments = 12;
            for (int i = 0; i < segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                Vector3D offset = radius * (Math.Cos(angle) * u + Math.Sin(angle) * v);
                mesh.Positions.Add(p1 + offset);
                mesh.Positions.Add(p2 + offset);
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int i0 = baseIndex + 2 * i;
                int i1 = baseIndex + 2 * i + 1;
                int i2 = baseIndex + 2 * next;
                int i3 = baseIndex + 2 * next + 1;

                mesh.TriangleIndices.Add(i0);
                mesh.TriangleIndices.Add(i2);
                mesh.TriangleIndices.Add(i1);

                mesh.TriangleIndices.Add(i1);
                mesh.TriangleIndices.Add(i2);
                mesh.TriangleIndices.Add(i3);
            }
        }

        private void AddCone(MeshGeometry3D mesh, Point3D baseCenter, Point3D tip, double baseRadius)
        {
            int baseIndex = mesh.Positions.Count;
            Vector3D axis = tip - baseCenter;
            axis.Normalize();

            Vector3D perp = Math.Abs(axis.Y) > 0.9 ? new Vector3D(1, 0, 0) : new Vector3D(0, 1, 0);
            Vector3D u = Vector3D.CrossProduct(axis, perp);
            u.Normalize();
            Vector3D v = Vector3D.CrossProduct(axis, u);

            mesh.Positions.Add(tip);

            int segments = 12;
            for (int i = 0; i < segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                Vector3D offset = baseRadius * (Math.Cos(angle) * u + Math.Sin(angle) * v);
                mesh.Positions.Add(baseCenter + offset);
            }

            for (int i = 0; i < segments; i++)
            {
                mesh.TriangleIndices.Add(baseIndex);
                mesh.TriangleIndices.Add(baseIndex + 1 + i);
                mesh.TriangleIndices.Add(baseIndex + 1 + ((i + 1) % segments));
            }
        }

        private void AddGridPlane(Viewport3D viewport, Color color, double alpha)
        {
            var mesh = new MeshGeometry3D();
            double size = 3;
            int divisions = 10;
            double step = size * 2 / divisions;

            for (int i = 0; i <= divisions; i++)
            {
                double pos = -size + i * step;
                AddCylinder(mesh, new Point3D(-size, 0, pos), new Point3D(size, 0, pos), 0.01);
                AddCylinder(mesh, new Point3D(pos, 0, -size), new Point3D(pos, 0, size), 0.01);
            }

            var brush = new SolidColorBrush(color) { Opacity = alpha };
            var material = new DiffuseMaterial(brush);
            var model = new GeometryModel3D(mesh, material);
            viewport.Children.Add(new ModelVisual3D { Content = model });
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            for (int step = 0; step < stepsPerFrame; step++)
            {
                // FitzHugh-Nagumo equations (Runge-Kutta 4th order)
                double k1v = v - Math.Pow(v, 3) / 3.0 - w + I;
                double k1w = epsilon * (v + a - b * w);

                double k2v = (v + dt * k1v / 2) - Math.Pow(v + dt * k1v / 2, 3) / 3.0 - (w + dt * k1w / 2) + I;
                double k2w = epsilon * ((v + dt * k1v / 2) + a - b * (w + dt * k1w / 2));

                double k3v = (v + dt * k2v / 2) - Math.Pow(v + dt * k2v / 2, 3) / 3.0 - (w + dt * k2w / 2) + I;
                double k3w = epsilon * ((v + dt * k2v / 2) + a - b * (w + dt * k2w / 2));

                double k4v = (v + dt * k3v) - Math.Pow(v + dt * k3v, 3) / 3.0 - (w + dt * k3w) + I;
                double k4w = epsilon * ((v + dt * k3v) + a - b * (w + dt * k3w));

                v += dt * (k1v + 2 * k2v + 2 * k3v + k4v) / 6.0;
                w += dt * (k1w + 2 * k2w + 2 * k3w + k4w) / 6.0;
                t += dt;

                trajectoryPoints.Add(new Point3D(v, w, t / 8.0));
            }

            if (trajectoryPoints.Count > maxPoints)
            {
                trajectoryPoints.RemoveRange(0, stepsPerFrame);
            }

            UpdateTrajectoryMesh();
        }

        private void UpdateTrajectoryMesh()
        {
            trajectoryMesh.Positions.Clear();
            trajectoryMesh.TriangleIndices.Clear();

            if (trajectoryPoints.Count < 2) return;

            double radius = 0.025;

            for (int i = 0; i < trajectoryPoints.Count - 1; i++)
            {
                AddTubeSegment(trajectoryMesh, trajectoryPoints[i], trajectoryPoints[i + 1], radius);
            }
        }

        private void AddTubeSegment(MeshGeometry3D mesh, Point3D p1, Point3D p2, double radius)
        {
            int baseIndex = mesh.Positions.Count;
            Vector3D direction = p2 - p1;
            direction.Normalize();

            Vector3D perp = Math.Abs(direction.Y) > 0.9 ? new Vector3D(1, 0, 0) : new Vector3D(0, 1, 0);
            Vector3D u = Vector3D.CrossProduct(direction, perp);
            u.Normalize();
            Vector3D v = Vector3D.CrossProduct(direction, u);

            int segments = 8;
            for (int j = 0; j <= 1; j++)
            {
                var center = j == 0 ? p1 : p2;
                for (int i = 0; i < segments; i++)
                {
                    double angle = 2 * Math.PI * i / segments;
                    Vector3D offset = radius * (Math.Cos(angle) * u + Math.Sin(angle) * v);
                    mesh.Positions.Add(center + offset);
                }
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                mesh.TriangleIndices.Add(baseIndex + i);
                mesh.TriangleIndices.Add(baseIndex + segments + i);
                mesh.TriangleIndices.Add(baseIndex + next);

                mesh.TriangleIndices.Add(baseIndex + next);
                mesh.TriangleIndices.Add(baseIndex + segments + i);
                mesh.TriangleIndices.Add(baseIndex + segments + next);
            }
        }

        private void ResetSimulation()
        {
            v = 0.1;
            w = 0.0;
            t = 0.0;
            trajectoryPoints.Clear();
        }
    }
}