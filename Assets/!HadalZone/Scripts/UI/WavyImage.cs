using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;
#if UNITY_2017_4 || UNITY_2018_2_OR_NEWER
using UnityEngine.U2D;
#endif

#if UNITY_EDITOR
using UnityEditor;

// Custom Editor to order the variables in the Inspector similar to Image component
[CustomEditor( typeof( WavyImage ) )]
[CanEditMultipleObjects]
public class WavyImageEditor : Editor
{
	private SerializedProperty colorProp, spriteProp, preserveAspectProp, waveSpeedIgnoresTimeScaleProp;
	private GUIContent spriteLabel;

	private List<WavyImage> wavyImages;
	private bool inspectingAsset;

	internal static bool previewInEditor;

	private void OnEnable()
	{
		Object[] _targets = targets;
		wavyImages = new List<WavyImage>( _targets.Length );
		for( int i = 0; i < _targets.Length; i++ )
		{
			WavyImage wavyImage = _targets[i] as WavyImage;
			if( wavyImage )
			{
				wavyImages.Add( wavyImage );
				inspectingAsset |= AssetDatabase.Contains( wavyImage );
			}
		}

		colorProp = serializedObject.FindProperty( "m_Color" );
		spriteProp = serializedObject.FindProperty( "m_Sprite" );
		preserveAspectProp = serializedObject.FindProperty( "m_PreserveAspect" );
		waveSpeedIgnoresTimeScaleProp = serializedObject.FindProperty( "m_WaveSpeedIgnoresTimeScale" );

		spriteLabel = new GUIContent( "Source Image" );
	}

	private void OnDisable()
	{
		if( previewInEditor )
		{
			previewInEditor = false;
			RefreshWavyImages();
		}

		EditorApplication.update -= PreviewInEditor;
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField( colorProp );
		EditorGUILayout.PropertyField( spriteProp, spriteLabel );
		if( spriteProp.hasMultipleDifferentValues || spriteProp.objectReferenceValue )
		{
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField( preserveAspectProp );
			EditorGUI.indentLevel--;
		}

		DrawPropertiesExcluding( serializedObject, "m_Script", "m_Color", "m_Sprite", "m_PreserveAspect", "m_WaveSpeedIgnoresTimeScale", "m_OnCullStateChanged" );

		EditorGUI.indentLevel++;
		EditorGUILayout.PropertyField( waveSpeedIgnoresTimeScaleProp );
		EditorGUI.indentLevel--;

		serializedObject.ApplyModifiedProperties();

		if( !EditorApplication.isPlayingOrWillChangePlaymode && !inspectingAsset )
		{
			EditorGUILayout.Space();

			EditorGUI.BeginChangeCheck();
			previewInEditor = GUILayout.Toggle( previewInEditor, "Preview In Editor", GUI.skin.button );
			if( EditorGUI.EndChangeCheck() )
			{
				if( previewInEditor )
				{
					EditorApplication.update -= PreviewInEditor;
					EditorApplication.update += PreviewInEditor;
				}
				else
				{
					EditorApplication.update -= PreviewInEditor;
					RefreshWavyImages();
					EditorApplication.delayCall += RefreshWavyImages; // Preview is sometimes not reset immediately without this call
				}
			}
		}
	}

	private void PreviewInEditor()
	{
		if( EditorApplication.isPlayingOrWillChangePlaymode )
		{
			previewInEditor = false;
			EditorApplication.update -= PreviewInEditor;
		}
		else
			RefreshWavyImages();
	}

	private void RefreshWavyImages()
	{
		EditorApplication.QueuePlayerLoopUpdate();
	}
}
#endif

[RequireComponent( typeof( CanvasRenderer ) )]
[AddComponentMenu( "UI/Wavy Image", 12 )]
public class WavyImage : MaskableGraphic, ILayoutElement
{
	[SerializeField]
	private Sprite m_Sprite;
	public Sprite sprite
	{
		get { return m_Sprite; }
		set
		{
			if( m_Sprite )
			{
				if( m_Sprite != value )
				{
					m_Sprite = value;

					SetAllDirty();
					TrackImage();
				}
			}
			else if( value )
			{
				m_Sprite = value;

				SetAllDirty();
				TrackImage();
			}
		}
	}

	[SerializeField]
	private bool m_PreserveAspect;
	public bool preserveAspect
	{
		get { return m_PreserveAspect; }
		set { if( m_PreserveAspect != value ) { m_PreserveAspect = value; SetVerticesDirty(); } }
	}

	[Header( "Wave Animation" )]
	[Tooltip( "Number of rows and columns in the generated grid. The larger this value is, the smoother the wave animation will be but the calculations will also take more time." )]
	[Range( 0, 10 )]
	[SerializeField]
	private int m_WaveSegments = 3;
	public int waveSegments
	{
		get { return m_WaveSegments; }
		set { if( m_WaveSegments != value ) { m_WaveSegments = value; SetVerticesDirty(); } }
	}

	[Range( -0.5f, 0.5f )]
	[SerializeField]
	private float m_WaveStrength = 0.15f;
	public float waveStrength
	{
		get { return m_WaveStrength; }
		set { if( m_WaveStrength != value ) { m_WaveStrength = value; SetVerticesDirty(); } }
	}

	[Tooltip( "How far the wave mesh extends beyond the sprite. The larger this value is, the further the waves spill out of the sprite on all sides." )]
	[Range( 0f, 0.5f )]
	[SerializeField]
	private float m_WaveExtend = 0.15f;
	public float waveExtend
	{
		get { return m_WaveExtend; }
		set { if( m_WaveExtend != value ) { m_WaveExtend = value; SetVerticesDirty(); } }
	}

	[Tooltip( "Number of wave crests across the sprite. Higher values produce smaller and more frequent waves." )]
	[Range( 0.5f, 8f )]
	[SerializeField]
	private float m_WaveCount = 2f;
	public float waveCount
	{
		get { return m_WaveCount; }
		set { if( m_WaveCount != value ) { m_WaveCount = value; SetVerticesDirty(); } }
	}

	[Tooltip( "How transparent the wave crests become as they extend beyond the sprite, so the image itself does not look enlarged." )]
	[Range( 0f, 1f )]
	[SerializeField]
	private float m_WaveFade = 0.85f;
	public float waveFade
	{
		get { return m_WaveFade; }
		set { if( m_WaveFade != value ) { m_WaveFade = value; SetVerticesDirty(); } }
	}

	[Header( "Wave Sides" )]
	[Tooltip( "Which sides of the sprite the wave effect is applied to." )]
	[SerializeField]
	private bool m_WaveTop = true;
	public bool waveTop
	{
		get { return m_WaveTop; }
		set { if( m_WaveTop != value ) { m_WaveTop = value; SetVerticesDirty(); } }
	}

	[SerializeField]
	private bool m_WaveBottom = true;
	public bool waveBottom
	{
		get { return m_WaveBottom; }
		set { if( m_WaveBottom != value ) { m_WaveBottom = value; SetVerticesDirty(); } }
	}

	[SerializeField]
	private bool m_WaveLeft = true;
	public bool waveLeft
	{
		get { return m_WaveLeft; }
		set { if( m_WaveLeft != value ) { m_WaveLeft = value; SetVerticesDirty(); } }
	}

	[SerializeField]
	private bool m_WaveRight = true;
	public bool waveRight
	{
		get { return m_WaveRight; }
		set { if( m_WaveRight != value ) { m_WaveRight = value; SetVerticesDirty(); } }
	}

	[Tooltip( "As this value increases, the wave animation will become more 'chaotic' because vertices will start moving in more random directions." )]
	[Range( 0f, 1f )]
	[SerializeField]
	private float m_WaveDiversity = 0.5f;
	private float waveDiversitySqrt; // The visual impact of waveDiversity's square root seems to be more uniform than waveDiversity itself
	public float waveDiversity
	{
		get { return m_WaveDiversity; }
		set { if( m_WaveDiversity != value ) { m_WaveDiversity = value; waveDiversitySqrt = Mathf.Sqrt( value ); SetVerticesDirty(); } }
	}

	[SerializeField]
	private float m_WaveSpeed = 0.5f;
	public float waveSpeed
	{
		get { return m_WaveSpeed; }
		set { if( m_WaveSpeed != value ) { m_WaveSpeed = value; SetVerticesDirty(); } }
	}

	[SerializeField]
	private bool m_WaveSpeedIgnoresTimeScale = true;
	public bool waveSpeedIgnoresTimeScale
	{
		get { return waveSpeedIgnoresTimeScale; }
		set { waveSpeedIgnoresTimeScale = value; }
	}

	private float waveAnimationTime;

	public override Texture mainTexture { get { return m_Sprite ? m_Sprite.texture : s_WhiteTexture; } }

	public float pixelsPerUnit
	{
		get
		{
			float spritePixelsPerUnit = 100;
			if( m_Sprite )
				spritePixelsPerUnit = m_Sprite.pixelsPerUnit;

			float referencePixelsPerUnit = 100;
			if( canvas )
				referencePixelsPerUnit = canvas.referencePixelsPerUnit;

			return spritePixelsPerUnit / referencePixelsPerUnit;
		}
	}

	public override Material material
	{
		get
		{
			if( m_Material != null )
				return m_Material;

			if( m_Sprite && m_Sprite.associatedAlphaSplitTexture != null )
			{
#if UNITY_EDITOR
				if( Application.isPlaying )
#endif
					return Image.defaultETC1GraphicMaterial;
			}

			return defaultMaterial;
		}
		set { base.material = value; }
	}

	protected override void Awake()
	{
		base.Awake();
		waveDiversitySqrt = Mathf.Sqrt( m_WaveDiversity );
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		TrackImage();
	}

	protected override void OnDisable()
	{
		base.OnDisable();

		if( m_Tracked )
			UnTrackImage();
	}

	private bool HasWaveEffect()
	{
		return m_WaveSegments > 0 && m_WaveExtend > 0f && m_WaveSpeed != 0f && m_WaveStrength != 0f
			&& ( m_WaveTop || m_WaveBottom || m_WaveLeft || m_WaveRight );
	}

	private void Update()
	{
		if( HasWaveEffect() )
			SetVerticesDirty();
	}

	protected override void OnPopulateMesh( VertexHelper vh )
	{
		vh.Clear();

		Color32 color32 = color;
		Rect rect = GetPixelAdjustedRect();

		Vector4 uv;
		float bottomLeftX, bottomLeftY, width, height;
		if( m_Sprite )
		{
			uv = DataUtility.GetOuterUV( m_Sprite );
			Vector4 padding = DataUtility.GetPadding( m_Sprite );
			Vector2 size = m_Sprite.rect.size;

			int spriteW = Mathf.RoundToInt( size.x );
			int spriteH = Mathf.RoundToInt( size.y );

			if( m_PreserveAspect && size.sqrMagnitude > 0f )
			{
				float spriteRatio = size.x / size.y;
				float rectRatio = rect.width / rect.height;

				if( spriteRatio > rectRatio )
				{
					float oldHeight = rect.height;
					rect.height = rect.width * ( 1f / spriteRatio );
					rect.y += ( oldHeight - rect.height ) * rectTransform.pivot.y;
				}
				else
				{
					float oldWidth = rect.width;
					rect.width = rect.height * spriteRatio;
					rect.x += ( oldWidth - rect.width ) * rectTransform.pivot.x;
				}
			}

			bottomLeftX = rect.x + rect.width * padding.x / spriteW;
			bottomLeftY = rect.y + rect.height * padding.y / spriteH;
			width = rect.width * ( spriteW - padding.z - padding.x ) / spriteW;
			height = rect.height * ( spriteH - padding.w - padding.y ) / spriteH;
		}
		else
		{
			uv = Vector4.zero;

			bottomLeftX = rect.x;
			bottomLeftY = rect.y;
			width = rect.width;
			height = rect.height;
		}

#if UNITY_EDITOR
		if( ( !Application.isPlaying && !WavyImageEditor.previewInEditor ) || !HasWaveEffect() )
#else
		if( !HasWaveEffect() )
#endif
		{
			// Generate normal Image
			vh.AddVert( new Vector3( bottomLeftX, bottomLeftY, 0f ), color32, new Vector2( uv.x, uv.y ) );
			vh.AddVert( new Vector3( bottomLeftX, bottomLeftY + height, 0f ), color32, new Vector2( uv.x, uv.w ) );
			vh.AddVert( new Vector3( bottomLeftX + width, bottomLeftY + height, 0f ), color32, new Vector2( uv.z, uv.w ) );
			vh.AddVert( new Vector3( bottomLeftX + width, bottomLeftY, 0f ), color32, new Vector2( uv.z, uv.y ) );

			vh.AddTriangle( 0, 1, 2 );
			vh.AddTriangle( 2, 3, 0 );
		}
		else
		{
			// Generate multiple small quads and move their vertices
			float invWaveSegments = 1f / m_WaveSegments;
			waveAnimationTime += ( m_WaveSpeedIgnoresTimeScale ? Time.unscaledDeltaTime : Time.deltaTime ) * m_WaveSpeed;

#if UNITY_EDITOR
			waveDiversitySqrt = Mathf.Sqrt( m_WaveDiversity );
#endif

			float widthOffset = m_WaveStrength * width;
			float heightOffset = m_WaveStrength * height;

			// Extend the mesh bounds only on the sides that have the wave effect enabled
			float extendX = width * m_WaveExtend;
			float extendY = height * m_WaveExtend;
			float extL = m_WaveLeft ? extendX : 0f;
			float extR = m_WaveRight ? extendX : 0f;
			float extB = m_WaveBottom ? extendY : 0f;
			float extT = m_WaveTop ? extendY : 0f;

			float minX = bottomLeftX - extL;
			float minY = bottomLeftY - extB;
			float spanX = width + extL + extR;
			float spanY = height + extB + extT;

			// Normalized bounds of the sprite area inside the mesh's [0..1] space
			float leftEdge = extL / spanX;
			float rightEdge = 1f - extR / spanX;
			float bottomEdge = extB / spanY;
			float topEdge = 1f - extT / spanY;

			float k = Mathf.PI * 2f * m_WaveCount * waveDiversitySqrt;
			float t = waveAnimationTime;

			for( int i = 0; i <= m_WaveSegments; i++ )
			{
				for( int j = 0; j <= m_WaveSegments; j++ )
				{
					float normalizedX = j * invWaveSegments;
					float normalizedY = i * invWaveSegments;

					// How far this vertex is outside of the sprite, per side (0 inside, 1 at the outer mesh edge)
					float outL = leftEdge > 0f && normalizedX < leftEdge ? ( leftEdge - normalizedX ) / leftEdge : 0f;
					float outR = rightEdge < 1f && normalizedX > rightEdge ? ( normalizedX - rightEdge ) / ( 1f - rightEdge ) : 0f;
					float outB = bottomEdge > 0f && normalizedY < bottomEdge ? ( bottomEdge - normalizedY ) / bottomEdge : 0f;
					float outT = topEdge < 1f && normalizedY > topEdge ? ( normalizedY - topEdge ) / ( 1f - topEdge ) : 0f;

					float sideX = Mathf.Max( outL, outR );
					float sideY = Mathf.Max( outB, outT );
					float outMax = Mathf.Max( sideX, sideY );

					// Clamp the UVs to the sprite area so the protruding band does not sample outside of the texture
					float spriteX = leftEdge > 0f || rightEdge < 1f ? Mathf.Clamp01( ( normalizedX - leftEdge ) / ( rightEdge - leftEdge ) ) : normalizedX;
					float spriteY = bottomEdge > 0f || topEdge < 1f ? Mathf.Clamp01( ( normalizedY - bottomEdge ) / ( topEdge - bottomEdge ) ) : normalizedY;
					Vector2 _uv = new Vector2( uv.z * spriteX + uv.x * ( 1f - spriteX ), uv.w * spriteY + uv.y * ( 1f - spriteY ) );

					// Realistic traveling waves: horizontal waves roll along the left/right sides, vertical waves along the top/bottom sides
					float dxWave = Mathf.Sin( normalizedY * k - t ) + 0.5f * Mathf.Sin( normalizedX * k * 0.6f - t * 1.4f );
					float dyWave = Mathf.Sin( normalizedX * k - t ) + 0.5f * Mathf.Sin( normalizedY * k * 0.6f - t * 1.4f );

					Vector3 _position = new Vector3( minX + spanX * normalizedX, minY + spanY * normalizedY, 0f );
					_position.x += dxWave * widthOffset * sideX * ( 1f + sideX );
					_position.y += dyWave * heightOffset * sideY * ( 1f + sideY );

					// Fade the protruding band so the image itself does not look enlarged
					Color32 vertexColor = color32;
					if( outMax > 0f )
						vertexColor.a = ( byte )Mathf.Clamp( color32.a * ( 1f - outMax * m_WaveFade ), 0f, 255f );

					vh.AddVert( _position, vertexColor, _uv );
				}
			}

			for( int i = 0; i < m_WaveSegments; i++ )
			{
				int firstTriangle = i * ( m_WaveSegments + 1 );
				for( int j = 0; j < m_WaveSegments; j++ )
				{
					int triangle = firstTriangle + j;
					int aboveTriangle = triangle + m_WaveSegments + 1;

					vh.AddTriangle( triangle, aboveTriangle, aboveTriangle + 1 );
					vh.AddTriangle( aboveTriangle + 1, triangle + 1, triangle );
				}
			}
		}
	}

	int ILayoutElement.layoutPriority { get { return 0; } }
	float ILayoutElement.minWidth { get { return 0f; } }
	float ILayoutElement.minHeight { get { return 0f; } }
	float ILayoutElement.flexibleWidth { get { return -1; } }
	float ILayoutElement.flexibleHeight { get { return -1; } }
	float ILayoutElement.preferredWidth { get { return m_Sprite ? ( m_Sprite.rect.size.x / pixelsPerUnit ) : 0f; } }
	float ILayoutElement.preferredHeight { get { return m_Sprite ? ( m_Sprite.rect.size.y / pixelsPerUnit ) : 0f; } }
	void ILayoutElement.CalculateLayoutInputHorizontal() { }
	void ILayoutElement.CalculateLayoutInputVertical() { }

	// Whether this is being tracked for Atlas Binding
	private bool m_Tracked = false;

#if UNITY_2017_4 || UNITY_2018_2_OR_NEWER
	private static List<WavyImage> m_TrackedTexturelessImages = new List<WavyImage>();
	private static bool s_Initialized;
#endif

	private void TrackImage()
	{
		if( m_Sprite != null && m_Sprite.texture == null )
		{
#if UNITY_2017_4 || UNITY_2018_2_OR_NEWER
			if( !s_Initialized )
			{
				SpriteAtlasManager.atlasRegistered += RebuildImage;
				s_Initialized = true;
			}

			m_TrackedTexturelessImages.Add( this );
#endif
			m_Tracked = true;
		}
	}

	private void UnTrackImage()
	{
#if UNITY_2017_4 || UNITY_2018_2_OR_NEWER
		m_TrackedTexturelessImages.Remove( this );
#endif
		m_Tracked = false;
	}

#if UNITY_2017_4 || UNITY_2018_2_OR_NEWER
	private static void RebuildImage( SpriteAtlas spriteAtlas )
	{
		for( int i = m_TrackedTexturelessImages.Count - 1; i >= 0; i-- )
		{
			WavyImage image = m_TrackedTexturelessImages[i];
			if( spriteAtlas.CanBindTo( image.sprite ) )
			{
				image.SetAllDirty();
				m_TrackedTexturelessImages.RemoveAt( i );
			}
		}
	}
#endif
}