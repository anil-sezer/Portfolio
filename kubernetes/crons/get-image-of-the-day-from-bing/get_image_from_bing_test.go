package main

import "testing"

func TestBuildAltText(t *testing.T) {
	tests := []struct {
		name      string
		title     string
		copyright string
		want      string
	}{
		{
			name:      "both title and copyright present",
			title:     "Autumn in the Alps",
			copyright: "Larch trees in autumn, Swiss Alps (© Getty Images)",
			want:      "Autumn in the Alps - Larch trees in autumn, Swiss Alps (© Getty Images)",
		},
		{
			name:      "copyright starts with title",
			title:     "Swiss Alps",
			copyright: "Swiss Alps in autumn (© Getty Images)",
			want:      "Swiss Alps in autumn (© Getty Images)",
		},
		{
			name:      "only copyright present",
			title:     "",
			copyright: "Larch trees in autumn (© Getty Images)",
			want:      "Larch trees in autumn (© Getty Images)",
		},
		{
			name:      "only title present",
			title:     "Autumn in the Alps",
			copyright: "",
			want:      "Autumn in the Alps",
		},
		{
			name:      "both empty",
			title:     "   ",
			copyright: "",
			want:      "Daily featured background photograph from Bing Wallpaper",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			got := buildAltText(tt.title, tt.copyright)
			if got != tt.want {
				t.Errorf("buildAltText(%q, %q) = %q, want %q", tt.title, tt.copyright, got, tt.want)
			}
		})
	}
}
