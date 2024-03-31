using System;
using System.Collections.Generic;

namespace Avanpost.Interviews.Task.Integration.SandBox.Connector.Models;

public partial class Password
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string UserPassword { get; set; } = null!;
}
