namespace Vatsim.Vatis.Ui;

public interface ICloseable
{
    void Close(object? dialogResult);

    void Close();
}