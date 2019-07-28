CREATE FUNCTION GetPersonsCount()
RETURNS int
AS
BEGIN
	declare @cnt int

	select @cnt = count(*) from dbo.Person

	return @cnt
END
