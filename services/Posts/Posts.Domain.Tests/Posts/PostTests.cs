using FluentAssertions;
using Venture.Domain.Posts;
using Venture.Domain.Posts.Events;
using Venture.Domain.Posts.ValueObjects;
using Venture.Domain.Tests.TestData;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Domain.Tests.Posts;

public class PostTests
{
    private static Post CreatePost(UserId authorId) =>
        Post.Publish(authorId, PostContent.Create("Первый пост в Venture").Value, DomainFactory.Now).Value;

    [Fact]
    public void Publish_ShouldCreatePublishedPost_AndRaiseEvent()
    {
        var authorId = UserId.New();

        var post = CreatePost(authorId);

        post.AuthorId.Should().Be(authorId);
        post.IsDeleted.Should().BeFalse();
        post.EditedAtUtc.Should().BeNull();
        post.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PostPublishedDomainEvent>();
    }

    [Fact]
    public void Edit_ShouldUpdateContent()
    {
        var authorId = UserId.New();
        var post = CreatePost(authorId);
        var editedAt = DomainFactory.Now.AddMinutes(5);

        post.Edit(authorId, PostContent.Create("Обновлённый текст").Value, editedAt);

        post.Content.Value.Should().Be("Обновлённый текст");
        post.EditedAtUtc.Should().Be(editedAt);
    }

    [Fact]
    public void Edit_ShouldFail_WhenEditorIsNotAuthor()
    {
        var post = CreatePost(UserId.New());

        var result = post.Edit(UserId.New(), PostContent.Create("Чужой текст").Value, DomainFactory.Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Be("изменять пост может только его автор");
    }

    [Fact]
    public void Delete_ShouldMarkPostAsDeleted()
    {
        var authorId = UserId.New();
        var post = CreatePost(authorId);

        post.Delete(authorId);

        post.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Edit_ShouldFail_WhenPostIsDeleted()
    {
        var authorId = UserId.New();
        var post = CreatePost(authorId);
        post.Delete(authorId);

        var result = post.Edit(authorId, PostContent.Create("Текст").Value, DomainFactory.Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Be("пост удалён");
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void CreatePostContent_ShouldFail_WhenContentIsEmpty(string content)
    {
        var result = PostContent.Create(content);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CreatePostContent_ShouldFail_WhenContentIsTooLong()
    {
        var result = PostContent.Create(new string('a', PostContent.MaxLength + 1));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CreatePostContent_ShouldTrimContent()
    {
        var content = PostContent.Create("   текст   ");

        content.Value.Value.Should().Be("текст");
    }

    [Fact]
    public void AddMedia_ShouldFail_WhenUrlIsDuplicated()
    {
        var authorId = UserId.New();
        var post = CreatePost(authorId);
        post.AddMedia(MediaUrl.Create("https://venture.dev/image.png").Value);

        var result = post.AddMedia(MediaUrl.Create("https://venture.dev/image.png").Value);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CreateMediaUrl_ShouldFail_WhenUrlIsNotAbsoluteHttp()
    {
        var result = MediaUrl.Create("ftp://venture.dev/image.png");

        result.IsFailure.Should().BeTrue();
    }
}
