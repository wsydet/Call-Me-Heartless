using System;
using Game.PathCleaning;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Game.UI
{
    /// <summary>将刷子轨迹转换为归一化擦除段，并显示状态层的真实泥渍遮罩。</summary>
    public sealed class PathCleaningWipeHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Func<bool> _canWipe;
        private Action<Vector2,Vector2> _wipe;
        private RectTransform _rect;
        private Image _image;
        private Texture2D _texture;
        private Sprite _sprite;
        private Color32[] _pixels,_sourcePixels;
        private Color _color;
        private int _revision=-1;
        private bool _painting;
        private Vector2 _previous;
        public void Configure(PathCleaningGame game,int index, Func<bool> canWipe, Action<Vector2,Vector2> wipe)
        {
            var spot=game.Spots[index];
            _canWipe=canWipe;_wipe=wipe;_rect=(RectTransform)transform;_image=GetComponent<Image>();
            ColorUtility.TryParseHtmlString(spot.Type.Color,out _color);
            int size=PathCleaningGame.MudResolution;
            if(!string.IsNullOrEmpty(spot.Type.SpritePath))
            {
                var source=_image.sprite;if(!source)throw new InvalidOperationException("泥渍图片缺失");
                _sourcePixels=ReadSprite(source,PathCleaningGame.MudSourceResolution);var mask=new bool[_sourcePixels.Length];
                for(int i=0;i<mask.Length;i++)mask[i]=_sourcePixels[i].a>8;
                game.ConfigureMudMask(index,mask,source.rect.width/source.rect.height);
            }
            _image.preserveAspect=false;
            _texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            _texture.name="MudMask";_texture.filterMode=FilterMode.Bilinear;_texture.wrapMode=TextureWrapMode.Clamp;
            _sprite=Sprite.Create(_texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
            _image.sprite=_sprite;_image.color=Color.white;_image.raycastTarget=true;
            _pixels=new Color32[size*size];Render(spot);
        }
        public void Render(PathCleaningGame.Spot spot)
        {
            if(!_texture||_revision==spot.MudRevision)return;_revision=spot.MudRevision;
            for(int i=0;i<_pixels.Length;i++)
            {
                if(!spot.IsDirtyPixel(i)){_pixels[i]=new Color32(0,0,0,0);continue;}
                if(_sourcePixels!=null){var source=_sourcePixels[spot.MudSourcePixel(i)];source.a=(byte)Mathf.RoundToInt(source.a*spot.MudOpacity(i));_pixels[i]=source;}
                else {int source=spot.MudSourcePixel(i);float u=(source%PathCleaningGame.MudSourceResolution+.5f)/PathCleaningGame.MudSourceResolution,v=(source/PathCleaningGame.MudSourceResolution+.5f)/PathCleaningGame.MudSourceResolution;
                    float shade=.85f+.18f*Mathf.PerlinNoise(u*6.3f+spot.Cell,v*6.3f)+.05f*Mathf.PerlinNoise(u*31,v*31);
                    var color=_color*shade;color.a=_color.a*spot.MudOpacity(i);_pixels[i]=color;}
            }
            _texture.SetPixels32(_pixels);_texture.Apply(false,false);
        }
        private static Color32[] ReadSprite(Sprite sprite,int resolution)
        {
            // GPU采样Sprite实际网格及UV：不要求Read/Write，兼容图集裁剪和旋转。
            var target=RenderTexture.GetTemporary(resolution,resolution,0,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;Mesh mesh=null;Material material=null;Texture2D readback=null;bool matrix=false;
            try
            {
                var shader=Resources.Load<Shader>("UI/Module/PathCleaning/Shaders/SpriteMaskReadback");
                if(!shader)throw new InvalidOperationException("泥渍遮罩采样Shader缺失");
                material=new Material(shader);material.mainTexture=sprite.texture;
                var vertices=sprite.vertices;var positions=new Vector3[vertices.Length];
                for(int i=0;i<vertices.Length;i++)positions[i]=new Vector3((vertices[i].x*sprite.pixelsPerUnit+sprite.pivot.x)/sprite.rect.width,(vertices[i].y*sprite.pixelsPerUnit+sprite.pivot.y)/sprite.rect.height,0);
                var triangles=sprite.triangles;var indices=new int[triangles.Length];for(int i=0;i<indices.Length;i++)indices[i]=triangles[i];
                mesh=new Mesh();mesh.vertices=positions;mesh.uv=sprite.uv;mesh.triangles=indices;
                RenderTexture.active=target;GL.Clear(true,true,Color.clear);GL.PushMatrix();matrix=true;GL.LoadOrtho();
                if(!material.SetPass(0))throw new InvalidOperationException("泥渍遮罩Shader不可用");
                Graphics.DrawMeshNow(mesh,Matrix4x4.identity);
                GL.PopMatrix();matrix=false;
                readback=new Texture2D(resolution,resolution,TextureFormat.RGBA32,false);
                readback.ReadPixels(new Rect(0,0,resolution,resolution),0,0);readback.Apply();return readback.GetPixels32();
            }
            finally
            {
                if(matrix)GL.PopMatrix();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
                ReleaseObject(mesh);ReleaseObject(material);ReleaseObject(readback);
            }
        }
        private static void ReleaseObject(UnityEngine.Object value){if(!value)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private bool Position(PointerEventData e,out Vector2 uv)
        {
            uv=default;if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect,e.position,e.pressEventCamera,out var point))return false;
            var r=_rect.rect;if(r.width<=0||r.height<=0)return false;
            uv=new Vector2((point.x-r.xMin)/r.width,(point.y-r.yMin)/r.height);return true;
        }
        public void OnPointerDown(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||_canWipe?.Invoke()!=true||!Position(e,out _previous))return;
            _painting=true;_wipe?.Invoke(_previous,_previous);
        }
        public void OnInitializePotentialDrag(PointerEventData e){e.useDragThreshold=false;}
        public void OnBeginDrag(PointerEventData e){}
        public void OnDrag(PointerEventData e)
        {
            if(!_painting)return;
            if(_canWipe?.Invoke()!=true){_painting=false;return;}
            if(!Position(e,out var current))return;var previous=_previous;_previous=current;_wipe?.Invoke(previous,current);
        }
        public void OnPointerUp(PointerEventData e){_painting=false;}
        public void OnEndDrag(PointerEventData e){_painting=false;}
        private void OnDisable(){_painting=false;}
        public void Release()
        {
            _painting=false;if(_image&&_image.sprite==_sprite)_image.sprite=null;
            if(_sprite){if(Application.isPlaying)Destroy(_sprite);else DestroyImmediate(_sprite);}_sprite=null;
            if(_texture){if(Application.isPlaying)Destroy(_texture);else DestroyImmediate(_texture);}_texture=null;_pixels=null;_sourcePixels=null;
        }
        private void OnDestroy(){Release();}
    }
}
