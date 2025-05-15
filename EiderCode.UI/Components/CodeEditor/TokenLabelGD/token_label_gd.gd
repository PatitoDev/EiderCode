extends Label;

var content;

func setLabel(value):
	content = value;

func _ready():
	text = content;
