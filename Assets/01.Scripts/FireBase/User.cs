
// User의 DataModel이다 라고 부릅니다.
public class User
{
    public string ID { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }

    public static User Empty = new User();

}
